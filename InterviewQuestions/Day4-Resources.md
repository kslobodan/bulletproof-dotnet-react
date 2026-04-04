# Day 4: Resources CRUD & Multi-Tenant Isolation

[← Back to Index](./README.md) | [← Previous: Day 3](./Day3-Authentication.md)

---

## Q: "Walk me through how CQRS is implemented for Resources. Why separate commands and queries?"

**A:** "CQRS separates write operations (Commands) from read operations (Queries) into different folders and handlers:

**Commands (Write Side):**

- `CreateResourceCommand`, `UpdateResourceCommand`, `DeleteResourceCommand`
- Each has its own Handler (business logic) and Validator (FluentValidation)
- Handlers modify database state and return simple responses
- Example: `CreateResourceCommandHandler` generates a Guid, sets TenantId from context, inserts to database

**Queries (Read Side):**

- `GetResourceByIdQuery`, `GetAllResourcesQuery`
- Only read data, never modify
- Can be optimized differently (caching, indexes, read replicas)
- Example: `GetAllResourcesQuery` supports pagination, filtering by ResourceType and IsActive

**Benefits in this project:**

1. **Separation of Concerns** - Write logic (validation, business rules) is separate from read logic (filtering, pagination)
2. **Optimization** - Queries can use different indexes or even different data stores without affecting writes
3. **Scalability** - Can scale read and write databases independently (read replicas for queries, master for commands)
4. **Testing** - Commands and queries tested independently with different concerns
5. **Clarity** - Looking at a handler, you immediately know if it changes data (Command) or just reads (Query)

**Real-world example:** In production, `GetAllResourcesQuery` could hit a read replica or Redis cache, while `CreateResourceCommand` writes to the master database. They're completely independent."

---

## Q: "How does BaseRepository<T> automatically filter by TenantId? What happens when GetAllAsync() is called?"

**A:** "The tenant filtering happens through a combination of inheritance, dependency injection, and Dapper extensions:

**Step-by-step flow when `GetAllAsync()` is called:**

```csharp
// 1. ResourceRepository extends BaseRepository<Resource>
public class ResourceRepository : BaseRepository<Resource>, IResourceRepository
{
    // No override needed - inherits GetAllAsync from BaseRepository
}

// 2. BaseRepository.GetAllAsync implementation
public async Task<IEnumerable<T>> GetAllAsync(...)
{
    var sql = $"SELECT * FROM {TableName} WHERE TenantId = @TenantId";
    return await connection.QueryWithTenantAsync<T>(_tenantContext, sql);
}

// 3. Dapper extension adds TenantId parameter
public static async Task<IEnumerable<T>> QueryWithTenantAsync<T>(
    this IDbConnection connection,
    ITenantContext tenantContext,
    string sql,
    object? parameters = null)
{
    var tenantId = tenantContext.TenantId; // Get from HTTP context
    var combinedParams = new DynamicParameters(parameters);
    combinedParams.Add("TenantId", tenantId); // Inject tenant filter
    return await connection.QueryAsync<T>(sql, combinedParams);
}
```

**How tenant isolation is enforced:**

1. **Tenant Resolution Middleware** extracts `X-Tenant-Id` header and sets `TenantContext.TenantId`
2. `BaseRepository` is injected with `ITenantContext` via constructor DI
3. Every query in `BaseRepository` includes `WHERE TenantId = @TenantId`
4. Even `GetByIdAsync` filters: `WHERE Id = @Id AND TenantId = @TenantId`

**Security guarantee:**
If Tenant A tries to access Tenant B's resource by ID, even if they guess the Guid correctly, the query returns NULL because the TenantId filter rejects it. The repository layer enforces isolation automatically—no risk of developers forgetting to add the filter.

**Interview follow-up answer:** 'What if you need to query across tenants?' → Create a separate repository (like `TenantRepository`) that doesn't extend `BaseRepository<T>` and doesn't include tenant filtering. Use it only for admin operations with proper authorization checks."

---

## Q: "Why add GetPagedAsync to the base IRepository<T> interface instead of just ResourceRepository?"

**A:** "I added `GetPagedAsync` to the base interface because pagination is a cross-cutting concern that virtually all entities need:

**Reasoning:**

1. **DRY Principle** - Resources, Bookings, Users all need pagination. Implement once in `BaseRepository<T>`, reuse everywhere
2. **Consistent API** - Every endpoint returns the same `PagedResult<T>` structure with `totalCount`, `pageNumber`, `pageSize`, `hasNextPage`
3. **Client-Friendly** - Frontend developers get predictable pagination metadata across all endpoints
4. **Standards** - Most RESTful APIs paginate collections to prevent loading thousands of records

**Implementation in BaseRepository:**

```csharp
public async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, ...)
{
    // 1. Get total count (filtered by tenant)
    var countSql = $"SELECT COUNT(*)::int FROM {TableName} WHERE TenantId = @TenantId";
    var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { TenantId });

    // 2. Calculate offset
    var offset = (pageNumber - 1) * pageSize;

    // 3. Get page of data
    var dataSql = $@"SELECT * FROM {TableName}
                     WHERE TenantId = @TenantId
                     ORDER BY CreatedAt DESC
                     LIMIT @PageSize OFFSET @Offset";

    var items = await connection.QueryAsync<T>(dataSql, new { TenantId, PageSize, Offset });

    // 4. Return PagedResult with metadata
    return new PagedResult<T>(items.ToList(), totalCount, pageNumber, pageSize);
}
```

**Trade-offs:**

**Pros:**

- Standardized pagination across all entities
- One implementation, tested once, works everywhere
- PagedResult metadata helps clients build UI (page numbers, disable prev/next buttons)

**Cons:**

- Not all entities need pagination (small lookup tables like Roles)
- Forces all repositories to implement it even if unused
- Offset pagination doesn't scale to millions of rows (cursor-based would be better at scale)

**Why it's the right choice for this project:**
For a booking system with thousands (not millions) of resources/bookings, offset pagination is sufficient and simple. The consistency benefit outweighs the 'interface bloat' cost. If scaling to millions of records, I'd switch to cursor-based pagination (keyset pagination) using indexed columns for WHERE clauses instead of OFFSET."

---

## Q: "Where in the request pipeline does FluentValidation execute? Why not validate inside the CommandHandler?"

**A:** "FluentValidation executes in a **MediatR Pipeline Behavior** BEFORE the handler runs:

**Request Pipeline Order:**

```
1. Controller receives HTTP request
2. Model Binding creates CreateResourceCommand
3. MediatR.Send(command)
   ↓
4. ValidationBehavior (MediatR Pipeline)
   ↓
5. FluentValidation runs CreateResourceCommandValidator
   ↓
   → If invalid: throw ValidationException (400 Bad Request)
   → If valid: continue to handler
   ↓
6. CreateResourceCommandHandler.Handle() executes
7. Return response to Controller
```

**Why this is better than validating in the handler:**

**Bad approach (validation in handler):**

```csharp
public async Task<CreateResourceResponse> Handle(CreateResourceCommand request, ...)
{
    // Handler cluttered with validation boilerplate
    if (string.IsNullOrWhiteSpace(request.Name))
        throw new ValidationException("Name is required");
    if (request.Name.Length > 200)
        throw new ValidationException("Name too long");
    if (request.Capacity <= 0)
        throw new ValidationException("Capacity must be positive");

    // Business logic buried below validation code
    var resource = new Resource { Name = request.Name, ... };
    await _repository.AddAsync(resource);
    return response;
}
```

**Good approach (FluentValidation in pipeline):**

```csharp
// Validator (separate file, reusable, declarative)
public class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ResourceType).NotEmpty();
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

// Handler (clean, focused on business logic)
public async Task<CreateResourceResponse> Handle(CreateResourceCommand request, ...)
{
    // Validation already passed - data is guaranteed valid
    var resource = new Resource
    {
        Id = Guid.NewGuid(),
        Name = request.Name, // Known to be valid
        TenantId = _tenantContext.TenantId,
        CreatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(resource);
    return new CreateResourceResponse { Resource = MapToDto(resource) };
}
```

**Benefits:**

1. **Separation of Concerns** - Validation rules separate from business logic
2. **Reusability** - Same validator can be used in API, background jobs, message handlers
3. **Testability** - Test validators independently: `var result = validator.Validate(command);`
4. **Fail Fast** - Invalid requests rejected BEFORE hitting database/business logic
5. **Consistency** - All commands/queries validated the same way via pipeline behavior
6. **Declarative** - Validation rules read like English, self-documenting

**Real-world example:** If we add a GraphQL API later, the same `CreateResourceCommandValidator` works without changes. The validation is decoupled from the transport layer (REST, GraphQL, gRPC)."

---

## Q: "Explain the middleware pipeline order. Why did /auth endpoints get blocked even with [AllowAnonymous]?"

**A:** "This was a critical bug I discovered during testing. The issue was middleware execution order:

**The Problem:**

`TenantResolutionMiddleware` runs BEFORE the authentication middleware in the pipeline. It was checking for `X-Tenant-Id` header on ALL requests except `/swagger` and `/health`. The `[AllowAnonymous]` attribute only affects the **Authentication/Authorization middleware**, NOT custom middleware.

**Middleware Pipeline Order:**

```
Request
  ↓
GlobalExceptionHandlerMiddleware (catches all exceptions)
  ↓
TenantResolutionMiddleware (was blocking /auth)
  ↓
AuthenticationMiddleware (evaluates [AllowAnonymous])
  ↓
AuthorizationMiddleware (evaluates [Authorize])
  ↓
Controller endpoint
```

**What happened:**

1. `POST /api/v1/auth/register-tenant` comes in (no X-Tenant-Id header)
2. GlobalExceptionHandler: passes through
3. TenantResolutionMiddleware: **'X-Tenant-Id required!' → 400 error**
4. Authentication middleware: never reached
5. `[AllowAnonymous]`: never evaluated

**The Fix:**

```csharp
// TenantResolutionMiddleware.cs
public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
{
    var path = context.Request.Path.Value?.ToLower();

    // Bypass tenant check for these endpoints
    if (path != null && (path.Contains("/swagger") ||
                         path.Contains("/health") ||
                         path.Contains("/auth")))
    {
        await _next(context); // Skip tenant resolution
        return;
    }

    // For all other endpoints, require X-Tenant-Id header
    var tenantIdHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
    // ... rest of validation
}
```

**Key Insights:**

1. **`[AllowAnonymous]` is NOT a middleware bypass** - It only tells Authorization middleware 'skip role checks'
2. **Middleware order matters** - Each middleware decides whether to call `_next()` or short-circuit
3. **Path-based bypasses are explicit** - `/auth` endpoints create/verify tenants, so they CAN'T require tenant ID yet
4. **Custom middleware is independent** - Doesn't know about controller attributes like `[Authorize]` or `[AllowAnonymous]`

**Similar real-world scenarios:**

- Rate limiting middleware that should bypass health checks
- CORS middleware that needs to handle OPTIONS preflight before authentication
- Request logging middleware that should log everything, even unauthorized requests

**Interview follow-up:** 'Could you move TenantResolutionMiddleware after authentication?' → No! We need TenantId BEFORE authorization checks. Authorization policies check tenant-admin roles, which require knowing the current tenant. The solution is explicit path bypasses for endpoints that don't have tenants yet (like registration)."

---

## Q: "Explain the database indexes on the Resources table. Why these specific indexes?"

**A:** "The Resources table has 5 indexes, each optimized for specific query patterns:

**Migration SQL:**

```sql
CREATE TABLE Resources (
    Id UUID PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    ResourceType VARCHAR(100) NOT NULL,
    TenantId UUID NOT NULL,
    IsActive BOOLEAN DEFAULT true,
    -- other columns
);

-- Indexes:
CREATE INDEX IX_Resources_TenantId ON Resources(TenantId);
CREATE INDEX IX_Resources_ResourceType ON Resources(ResourceType);
CREATE INDEX IX_Resources_IsActive ON Resources(IsActive);
CREATE INDEX IX_Resources_TenantId_IsActive ON Resources(TenantId, IsActive);
CREATE UNIQUE INDEX UQ_Resources_Name_TenantId ON Resources(Name, TenantId);
```

**Index Analysis:**

**1. IX_Resources_TenantId (Single-column index)**

- **Query:** `SELECT * FROM Resources WHERE TenantId = '...'`
- **Used by:** GetAllAsync (base query for all tenant data)
- **Why:** Most queries filter by tenant first, this is the most frequently used condition

**2. IX_Resources_ResourceType (Single-column index)**

- **Query:** `SELECT * FROM Resources WHERE ResourceType = 'MeetingRoom'`
- **Used by:** GetAllResourcesQuery with ResourceType filter
- **Why:** Users filter resources by type (meeting rooms vs equipment vs doctors)

**3. IX_Resources_IsActive (Single-column index)**

- **Query:** `SELECT * FROM Resources WHERE IsActive = true`
- **Used by:** Filtering active vs soft-deleted resources
- **Why:** UI typically shows only active resources by default

**4. IX_Resources_TenantId_IsActive (Composite index)**

- **Query:** `SELECT * FROM Resources WHERE TenantId = '...' AND IsActive = true`
- **Used by:** GetAllResourcesQuery default filter (tenant + active only)
- **Why:** This is the MOST COMMON query pattern - composite index is faster than using two single-column indexes
- **Cardinality:** Left-to-right index, so also works for queries filtering only by TenantId

**5. UQ_Resources_Name_TenantId (Unique constraint + index)**

- **Constraint:** Prevent duplicate resource names within a tenant
- **Example:** Tenant A can have 'Conference Room 1' and Tenant B can have 'Conference Room 1', but Tenant A can't have two resources with the same name
- **Why:** Business rule enforced at database level (not just application validation)

**Index Selection Strategy:**

- **Single-column indexes** for individual filters (ResourceType, IsActive)
- **Composite index** for most common combined filter (TenantId + IsActive)
- **Unique constraint** for business rule enforcement (name uniqueness per tenant)

**Trade-offs:**

**Pros:**

- Fast query performance on common filters
- Unique constraint prevents duplicate names at database level (safer than app-level validation)

**Cons:**

- Indexes slow down INSERT/UPDATE/DELETE (each write must update 5 indexes)
- Storage overhead (indexes take disk space)

**Why it's worth it:**
Booking systems are READ-heavy (browsing available resources) vs WRITE-light (CRUD operations are rare). The read performance gain outweighs the write cost.

**Interview follow-up:** 'How would you verify index usage?' → Use PostgreSQL's `EXPLAIN ANALYZE` to see query execution plans. Example:

````sql
EXPLAIN ANALYZE
SELECT * FROM Resources
WHERE TenantId = '4b47f363-8f8d-4dce-bcec-4ee66d2a2eb4'
AND IsActive = true;

-- Should show: Index Scan using IX_Resources_TenantId_IsActive
-- NOT: Seq Scan on Resources (sequential scan = no index used)
```"

---

## Q: "Why does DELETE use the AdminOnly policy? How is role-based authorization configured?"

**A:** "DELETE operations are restricted to TenantAdmin role because deleting resources has cascading effects and should be limited to administrators:

**Controller Implementation:**

```csharp
[HttpDelete("{id}")]
[Authorize(Policy = "AdminOnly")] // Requires TenantAdmin role
public async Task<IActionResult> DeleteResource(Guid id)
{
    var command = new DeleteResourceCommand { Id = id };
    var response = await _mediator.Send(command);
    return Ok(response);
}
````

**Authorization Configuration (Program.cs):**

```csharp
// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    // Only TenantAdmin can perform admin operations
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("TenantAdmin"));

    // Manager or TenantAdmin can manage resources/bookings
    options.AddPolicy("ManagerOrAbove", policy =>
        policy.RequireRole("TenantAdmin", "Manager"));

    // Default: All endpoints require authentication
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

**How it works at runtime:**

1. User logs in with `admin@acme.com` / `Admin1234`
2. `LoginCommandHandler` queries `UserRoles` table, finds user has `TenantAdmin` role
3. `JwtTokenService` generates JWT with role claim:
   ```
   {
     "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "TenantAdmin",
     "email": "admin@acme.com",
     "userId": "8b9337d6-...",
     ...
   }
   ```
4. User sends `DELETE /api/v1/resources/{id}` with `Authorization: Bearer {token}`
5. **Authentication Middleware** validates JWT signature and expiration
6. **Authorization Middleware** checks `[Authorize(Policy = "AdminOnly")]`:
   - Extracts role claim from JWT
   - Checks if role is "TenantAdmin"
   - If yes: allow request to reach controller
   - If no (e.g., role is "User"): return 403 Forbidden

**Why restrict DELETE to admins:**

1. **Data Safety** - Prevent accidental deletion by regular users
2. **Cascading Deletes** - If Bookings reference Resources with ON DELETE CASCADE, deleting a resource deletes all its bookings
3. **Audit Trail** - Admins are accountable for destructive operations
4. **Business Logic** - In real systems, 'delete' might mean 'archive' or require notifications to users with active bookings

**Role Hierarchy:**

- **TenantAdmin** - Full control within tenant (CRUD users, resources, bookings, delete operations)
- **Manager** - Manage resources and bookings (create, update, view) but cannot delete or manage users
- **User** - Create bookings for themselves, view available resources

**Interview follow-up:** 'What if a Manager tries to delete?' → Authorization middleware returns 403 Forbidden before the controller is reached. The DeleteResourceCommandHandler never executes. This is fail-safe - security enforced at middleware level, not just in business logic."

---

## Q: "You mentioned 'soft delete' in the code (IsActive column). What's the difference from hard delete?"

**A:** "Soft delete means marking records as inactive (IsActive = false) instead of physically removing them from the database:

**Hard Delete (current DELETE endpoint):**

```csharp
public async Task<DeleteResourceResponse> Handle(DeleteResourceCommand request, ...)
{
    await _repository.DeleteAsync(request.Id); // DELETE FROM Resources WHERE Id = @Id
    return new DeleteResourceResponse { Message = "Resource deleted successfully" };
}
```

**Soft Delete (better approach):**

```csharp
public async Task<DeleteResourceResponse> Handle(DeleteResourceCommand request, ...)
{
    var resource = await _repository.GetByIdAsync(request.Id);
    if (resource == null) throw new NotFoundException("Resource not found");

    resource.IsActive = false; // Mark as deleted, don't actually delete
    resource.UpdatedAt = DateTime.UtcNow;

    await _repository.UpdateAsync(resource); // UPDATE Resources SET IsActive = false
    return new DeleteResourceResponse { Message = "Resource deactivated successfully" };
}
```

**Comparison:**

| Aspect            | Hard Delete                            | Soft Delete                                    |
| ----------------- | -------------------------------------- | ---------------------------------------------- |
| **Database**      | DELETE FROM Resources                  | UPDATE Resources SET IsActive = false          |
| **Recovery**      | Impossible (unless from backup)        | Easy: UPDATE SET IsActive = true               |
| **Audit Trail**   | Lost forever                           | Preserved for reporting/compliance             |
| **Cascading**     | ON DELETE CASCADE removes related data | Related data remains, can be filtered          |
| **GDPR**          | Required for 'right to be forgotten'   | Not sufficient for data privacy laws           |
| **Query Changes** | No changes needed                      | All queries must filter: WHERE IsActive = true |

**When to use Soft Delete:**

1. **Audit Requirements** - Need to track what was deleted and when (healthcare, finance)
2. **Undo Capability** - Users can restore accidentally deleted items
3. **Historical Reporting** - Reports include deleted items to maintain historical accuracy
4. **Cascading Concerns** - Deleting a resource shouldn't delete all booking history

**When to use Hard Delete:**

1. **GDPR/Privacy Laws** - User requests data deletion (must physically remove)
2. **Storage Costs** - Data has no historical value, just taking up space
3. **Simplicity** - No need to filter `IsActive` in every query

**Implementation in this project:**

Resources table has `IsActive` column, but current DELETE endpoint does hard delete. To switch to soft delete:

1. Change `DeleteResourceCommandHandler` to update `IsActive = false`
2. Update `GetAllResourcesQuery` default filter: `WHERE TenantId = @TenantId AND IsActive = true`
3. Add optional `?includeInactive=true` parameter for admins to view deleted resources
4. Document that permanent deletion (hard delete) is done via background job after 30 days (or never, depending on requirements)

**Best Practice:** Start with soft delete by default, add hard delete endpoint for compliance scenarios (e.g., `/api/v1/resources/{id}/purge` for GDPR requests)."

---

## Q: "How would you test the multi-tenant isolation? Walk me through your verification approach."

**A:** "I tested multi-tenant isolation with both application-level tests and database verification:

**Test Approach:**

**1. Register Multiple Tenants:**

```powershell
# Tenant A: Acme Corp
Invoke-RestMethod POST /api/v1/auth/register-tenant
  → Got tenantId: 4b47f363-8f8d-4dce-bcec-4ee66d2a2eb4
  → Got JWT token for Alice (TenantAdmin)

# Tenant B: TechCo
Invoke-RestMethod POST /api/v1/auth/register-tenant
  → Got tenantId: fa6b63f7-55ae-4660-b1de-13a7d0258902
  → Got JWT token for Bob (TenantAdmin)

# Tenant C: GlobalCorp
Invoke-RestMethod POST /api/v1/auth/register-tenant
  → Got tenantId: e13ea0c3-b658-424b-91da-d286df05703e
  → Got JWT token for Charlie (TenantAdmin)
```

**2. Create Resources for Each Tenant:**

```powershell
# Acme creates a resource
POST /api/v1/resources
Headers:
  Authorization: Bearer {acme-jwt}
  X-Tenant-Id: 4b47f363-8f8d-4dce-bcec-4ee66d2a2eb4
Body: { name: "Acme Meeting Room 1", resourceType: "MeetingRoom", capacity: 8 }
→ Created resource ID: 47fd25cb-ad01-4e64-ba9a-4a4e28582b1c

# TechCo creates a resource
POST /api/v1/resources
Headers:
  Authorization: Bearer {techco-jwt}
  X-Tenant-Id: fa6b63f7-55ae-4660-b1de-13a7d0258902
Body: { name: "TechCo Lab Room", resourceType: "Laboratory", capacity: 4 }
→ Created resource ID: 081e727d-4801-46aa-8149-e3de03712d0b

# GlobalCorp creates a resource
POST /api/v1/resources
Headers:
  Authorization: Bearer {global-jwt}
  X-Tenant-Id: e13ea0c3-b658-424b-91da-d286df05703e
Body: { name: "GlobalCorp Boardroom", resourceType: "MeetingRoom", capacity: 20 }
→ Created resource ID: b1846791-47f2-4a27-860f-40977d1feb18
```

**3. Verify Database State (Ground Truth):**

```sql
SELECT id, name, resourcetype, tenantid FROM resources;

-- Result:
-- id                                    | name                  | resourcetype | tenantid
-- --------------------------------------+-----------------------+--------------+--------------------------------------
-- 47fd25cb-ad01-4e64-ba9a-4a4e28582b1c | Acme Meeting Room 1   | MeetingRoom  | 4b47f363-8f8d-4dce-bcec-4ee66d2a2eb4
-- 081e727d-4801-46aa-8149-e3de03712d0b | TechCo Lab Room       | Laboratory   | fa6b63f7-55ae-4660-b1de-13a7d0258902
-- b1846791-47f2-4a27-860f-40977d1feb18 | GlobalCorp Boardroom  | MeetingRoom  | e13ea0c3-b658-424b-91da-d286df05703e
```

✅ **Verification:** 3 resources, each with different tenantId as expected.

**4. Test Tenant A sees only their own resources:**

```powershell
GET /api/v1/resources
Headers:
  Authorization: Bearer {acme-jwt}
  X-Tenant-Id: 4b47f363-8f8d-4dce-bcec-4ee66d2a2eb4

Response:
{
  "items": [
    { "id": "47fd25cb-...", "name": "Acme Meeting Room 1", "tenantId": "4b47f363-..." }
  ],
  "totalCount": 1
}
```

✅ **Verification:** Acme sees only 1 resource (their own), not all 3.

**5. Test Cross-Tenant Access (Security Check):**

```powershell
# Try to access TechCo's resource using Acme's credentials
GET /api/v1/resources/081e727d-4801-46aa-8149-e3de03712d0b
Headers:
  Authorization: Bearer {acme-jwt}
  X-Tenant-Id: 4b47f363-8f8d-4dce-bcec-4ee66d2a2eb4

Response: 404 Not Found
# (or returns null because WHERE Id = @Id AND TenantId = @TenantId fails)
```

✅ **Verification:** Even with valid JWT and correct resource ID, cannot access other tenant's data!

**6. Test Invalid Tenant ID (Authorization Check):**

```powershell
GET /api/v1/resources
Headers:
  Authorization: Bearer {acme-jwt}
  X-Tenant-Id: fa6b63f7-55ae-4660-b1de-13a7d0258902  # TechCo's ID!

Response: 401 Unauthorized or 403 Forbidden
# (Middleware or authorization would reject JWT/tenant mismatch)
```

✅ **Note:** In current implementation, this might return empty list (no resources for TenantA in TenantB's context). Better approach: validate JWT tenantId claim matches X-Tenant-Id header in middleware.

**Verification Summary:**

| Test                                      | Expected | Result                 |
| ----------------------------------------- | -------- | ---------------------- |
| Database has 3 resources                  | ✅       | ✅ Verified via SQL    |
| Acme lists resources → sees 1             | ✅       | ✅ Only their resource |
| TechCo lists resources → sees 1           | ✅       | ✅ Only their resource |
| GlobalCorp lists resources → sees 1       | ✅       | ✅ Only their resource |
| Acme tries to GET TechCo's resource by ID | ❌ 404   | ✅ Access denied       |

**Security Guarantees:**

1. **Repository Layer** - `BaseRepository<T>` automatically adds `WHERE TenantId = @TenantId` to ALL queries
2. **No Bypasses** - Even GetByIdAsync, UpdateAsync, DeleteAsync include tenant filter
3. **Middleware Enforcement** - TenantContext.TenantId set from X-Tenant-Id header, used in all queries
4. **Developer Safety** - Impossible to accidentally write query without tenant filter (would have to bypass repository pattern entirely)

**What I'd test in Integration Tests:**

````csharp
[Fact]
public async Task GetAllResources_ReturnsOnlyTenantResources()
{
    // Arrange
    var tenantA = await CreateTenant("TenantA");
    var tenantB = await CreateTenant("TenantB");

    await CreateResource(tenantA.Id, "Resource A");
    await CreateResource(tenantB.Id, "Resource B");

    // Act
    var response = await GetResourcesWithTenantContext(tenantA.Id);

    // Assert
    response.Items.Should().HaveCount(1);
    response.Items[0].Name.Should().Be("Resource A");
}

[Fact]
public async Task GetResourceById_CrossTenant_ReturnsNotFound()
{
    // Arrange
    var tenantA = await CreateTenant("TenantA");
    var tenantB = await CreateTenant("TenantB");

    var resourceB = await CreateResource(tenantB.Id, "Resource B");

    // Act - Try to access Tenant B's resource with Tenant A's context
    var response = await GetResourceByIdWithTenantContext(tenantA.Id, resourceB.Id);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```"

---

[← Previous: Day 3](./Day3-Authentication.md) | [Next: Day 5 →](./Day5-Bookings.md) | [Back to Index](./README.md)
````
