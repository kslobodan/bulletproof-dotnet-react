# Interview Questions & Answers

**Purpose**: Common interview questions about this project, organized by development day.

**How to Use**: Review these before interviews to explain your technical decisions confidently.

---

## Day 1: Project Setup & Architecture

### Q: "Why did you choose Clean Architecture for this project?"

**A:** "I chose Clean Architecture because it provides clear separation of concerns and makes the codebase maintainable and testable. The dependency rule—where inner layers have no knowledge of outer layers—means my business logic (Domain) is completely independent of frameworks or databases. This makes it easy to swap implementations, like changing from Dapper to Entity Framework, without touching business logic. It also makes testing easier since I can mock dependencies at layer boundaries."

---

### Q: "Why did you use Docker and PostgreSQL?"

**A:** "I chose **PostgreSQL** because it's enterprise-grade, open-source, and has advanced features like JSONB and row-level security—perfect for multi-tenant architecture.

I used **Docker** for consistency and portability. The same `docker-compose.yml` works in development, CI/CD, and production. It eliminates 'works on my machine' issues and lets anyone clone the repo and start the database with one command.

This mirrors real-world microservices architecture where each service runs in containers orchestrated by Kubernetes or Docker Swarm."

---

### Q: "Explain the dependency flow in Clean Architecture."

**A:** "The dependency flow is: API → Infrastructure → Application → Domain.

- **Domain** has zero dependencies—it's pure business entities and rules
- **Application** depends only on Domain—it contains business logic and CQRS handlers
- **Infrastructure** depends on Application—it implements interfaces like repositories
- **API** depends on everything—it's the entry point that wires up dependency injection

This ensures the business logic is protected from framework changes and external dependencies."

---

### Q: "Why use VS Code instead of Visual Studio?"

**A:** "I use VS Code with C# Dev Kit for modern .NET development because it's lightweight, cross-platform, and encourages a CLI-first workflow. This gives me deeper understanding of what's happening under the hood rather than relying on GUI wizards. It's also the industry trend—most modern teams use VS Code for microservices and containerized applications. Plus, my workflow is portable across Windows, Linux, and Mac."

---

### Q: "What's the difference between dotnet new classlib and dotnet new webapi?"

**A:** "Both create .NET projects, but:

- **`dotnet new classlib`** creates a class library—just .NET code that compiles to a DLL. It's not executable on its own. I use this for Domain, Application, and Infrastructure layers.
- **`dotnet new webapi`** creates an ASP.NET Core Web API project—an executable application with controllers, middleware, and a web server. This is my API layer entry point.

In Clean Architecture, only the API layer is executable; the others are libraries it depends on."

---

### Q: "Why did you choose Dapper over Entity Framework?"

**A:** "I chose **Dapper** for several reasons:

1. **Performance** - Dapper is much faster than EF Core because it's a micro-ORM with minimal overhead
2. **Control** - I write SQL directly, so I understand exactly what queries hit the database. No hidden SQL generation or N+1 query surprises
3. **Learning** - It keeps my SQL skills sharp, which is valuable for database optimization and troubleshooting
4. **Simplicity** - For this project's size, Dapper's lightweight approach is perfect. No complex migrations or DbContext configuration

However, I acknowledge EF Core's advantages for rapid prototyping and change tracking. For enterprise apps with complex domain models, EF Core might be better. Dapper is ideal when you need performance and control."

---

### Q: "What is CQRS and why use MediatR?"

**A:** "**CQRS** (Command Query Responsibility Segregation) separates read operations (Queries) from write operations (Commands).

**Benefits:**

- **Clarity** - Clear separation between 'what changes data' and 'what reads data'
- **Scalability** - Can optimize read and write paths separately
- **Single Responsibility** - Each handler does one thing

**MediatR** implements the mediator pattern for CQRS:

- Controllers send commands/queries to MediatR: `await mediator.Send(new CreateBookingCommand(...))`
- MediatR finds the appropriate handler and executes it
- This decouples controllers from business logic—controllers become thin orchestrators

Example flow: **Controller → MediatR → UserCommandHandler → Repository → Database**"

---

### Q: "Explain your logging strategy with Serilog."

**A:** "I use **Serilog** for structured logging with multiple sinks:

1. **Console sink** - For development, see logs in real-time while running `dotnet run`
2. **File sink** - For persistence, daily rolling logs (`logs/log-20260403.txt`)

**Why structured logging?** Instead of string concatenation like `$"User {userId} failed"`, I use:

```csharp
Log.Information("User {UserId} login failed", userId);
```

This logs as JSON with properties, making it queryable in production logging systems like Elasticsearch or Seq. I can search 'all failures for UserId=123' instantly.

I also wrap startup in try-catch-finally to log fatal errors: `Log.Fatal(ex, "Application terminated")`"

---

### Q: "What is FluentValidation and why use it?"

**A:** "**FluentValidation** is a validation library that separates validation rules from models.

**Instead of attributes:**

```csharp
public class CreateBookingCommand {
    [Required] public int ResourceId { get; set; }
}
```

**I write validator classes:**

```csharp
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand> {
    public CreateBookingCommandValidator() {
        RuleFor(x => x.ResourceId).GreaterThan(0);
    }
}
```

**Benefits:**

- **Reusable** - Validators can be tested independently
- **Complex rules** - Can validate against database, check business rules, call services
- **Better errors** - Custom error messages per language/context
- **SOLID** - Separates validation concerns from commands

MediatR + FluentValidation work together: validate before the handler executes."

---

### Q: "Why use AutoMapper?"

**A:** "**AutoMapper** eliminates repetitive DTO-to-Entity mapping code.

**Without AutoMapper:**

```csharp
var entity = new Booking {
    Id = dto.Id,
    ResourceId = dto.ResourceId,
    StartTime = dto.StartTime,
    // ... 15 more lines
};
```

**With AutoMapper:**

```csharp
var entity = _mapper.Map<Booking>(dto);
```

**Benefits:**

- **DRY principle** - Define mappings once, use everywhere
- **Less boilerplate** - Reduces error-prone manual mapping
- **Convention-based** - Auto-maps properties with same names
- **Testable** - Can verify mapping configurations

**Trade-off:** Adds magic/indirection. I use it for simple mappings, but write manual mapping for complex transformations."

---

### Q: "How do you manage database connection strings securely?"

**A:** "I use ASP.NET Core's configuration system with multiple layers:

1. **Development**: `appsettings.Development.json` (gitignored) contains local credentials
2. **Production**: Environment variables or Azure Key Vault for secrets
3. **Connection string** retrieved via `Configuration.GetConnectionString("DefaultConnection")`

**Security:**

- Never commit production credentials to git
- Use Azure Managed Identity or AWS IAM roles in production
- appsettings.Development.json is in .gitignore
- Rotate credentials regularly

For this project, `postgres/postgres` is fine locally, but in production I'd use:

- Separate database user per service
- Least privilege permissions
- Connection string from environment variables"

---

## Day 2: Core Infrastructure & Multi-tenancy

### Q: "Explain your global error handling strategy."

**A:** "I implemented centralized error handling using custom middleware (`GlobalExceptionHandlerMiddleware`) that:

1. **Catches all unhandled exceptions** in a try-catch at the middleware level
2. **Logs errors** using Serilog with full stack traces
3. **Returns consistent JSON** responses:
   ```json
   {
     "StatusCode": 500,
     "Message": "An error occurred...",
     "Detailed": "[exception details]"
   }
   ```

**Benefits:**

- **Single source of truth** - All errors handled in one place
- **Consistent API responses** - Clients always get same error format
- **Clean controllers** - No try-catch in every action
- **Debugging-friendly** - Full error details in development, sanitized in production

The middleware is registered first in the pipeline so it wraps all subsequent middleware and catches errors from anywhere."

---

### Q: "Why did you implement API versioning?"

**A:** "I implemented API versioning using `Asp.Versioning` packages to support:

**Benefits:**

1. **Backward compatibility** - V1 clients still work when V2 is released
2. **Gradual migration** - Can deprecate old versions slowly
3. **Clear contracts** - Clients know exactly which version they're using

**Implementation:**

- **URL-based versioning**: `/api/v1/users` vs `/api/v2/users`
- **Default version**: v1.0 assumed if not specified
- **API Explorer integration**: Swagger automatically groups by version

**Configuration:**

```csharp
builder.Services.AddApiVersioning(options => {
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // Adds api-supported-versions header
});
```

Controllers decorated with `[ApiVersion("1.0")]` and routes use `api/v{version:apiVersion}/[controller]`."

---

### Q: "How did you configure Swagger for JWT authentication?"

**A:** "Currently, I have Swashbuckle configured with basic OpenAPI documentation. For JWT authentication (coming in Day 3), I'll add:

```csharp
services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "JWT Authorization header using Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        // ... JWT requirement
    });
});
```

This adds an **Authorize button** in Swagger UI where developers can:

1. Click 'Authorize'
2. Enter JWT token: `Bearer {token}`
3. Test protected endpoints directly from Swagger

**Benefits:**

- Developers can test authenticated endpoints without Postman
- Self-documenting API security
- Reduces support requests about authentication"

---

### Q: "What is the PagedResult<T> pattern and why use it?"

**A:** "**PagedResult<T>** is a generic wrapper for paginated API responses. Instead of returning raw arrays, I return:

```csharp
public class PagedResult<T> {
    public IEnumerable<T> Items { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => PageNumber < TotalPages;
    public bool HasPrevious => PageNumber > 1;
}
```

**Benefits:**

1. **Consistent pagination** across all endpoints
2. **Client-friendly** - Clients get metadata to build pagination UI
3. **Performance** - Only return needed rows (LIMIT/OFFSET in SQL)
4. **Predictable** - Same structure everywhere

**Example response:**

```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 156,
  "totalPages": 8,
  "hasNext": true,
  "hasPrevious": false
}
```

This prevents returning thousands of rows in one response and provides everything clients need for pagination controls."

---

### Q: "Why did you choose DbUp for database migrations?"

**A:** "I chose **DbUp** over Entity Framework Migrations or FluentMigrator because:

**Advantages:**

1. **SQL-first** - I write actual SQL files, so I have full control
2. **Simple** - Just SQL scripts with numbering (0001, 0002, etc.)
3. **Framework-agnostic** - Works with Dapper, ADO.NET, or EF Core
4. **Idempotent** - Tracks executed scripts in `schemaversions` table
5. **Startup integration** - Runs migrations on app start automatically

**How it works:**

```csharp
var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(assembly)
    .LogToConsole()
    .Build();
```

Scripts are **embedded resources** in the DLL (`.csproj` has `<EmbeddedResource Include="Data\Scripts\**\*.sql" />`), so migrations are part of the binary.

**Numbering:** `0001_InitialSchema.sql`, `0002_ConvertToUUID.sql` - executed in order, skipped if already run.

**Trade-off:** No automatic rollback like EF Migrations, but I rarely rollback in production—forward-only migrations are safer."

---

### Q: "Explain your repository pattern with Dapper."

**A:** "I implemented a **base repository pattern** with automatic tenant filtering:

**Architecture:**

```
IRepository<T> (Application)
    ↓
BaseRepository<T> (Infrastructure)
    ↓
UserRepository : BaseRepository<User>
```

**Key components:**

1. **IDbConnectionFactory** - Creates `NpgsqlConnection` instances
   - Interface in Application (dependency inversion)
   - Implementation in Infrastructure
   - Registered as **Singleton** (lightweight, stateless)

2. **BaseRepository<T>** - Abstract class with common CRUD
   - `GetByIdAsync()` with `WHERE Id = @Id AND TenantId = @TenantId`
   - `GetAllAsync()` with `WHERE TenantId = @TenantId`
   - `DeleteAsync()` with tenant filtering
   - All queries **automatically include TenantId**

3. **DapperExtensions** - Tenant-aware helpers
   - `QueryWithTenantAsync<T>()` - Adds TenantId to parameters
   - `ExecuteWithTenantAsync()` - For INSERT/UPDATE/DELETE

**Example:**

```csharp
public async Task<User?> GetByIdAsync(Guid id) {
    using var connection = _connectionFactory.CreateConnection();
    return await connection.QuerySingleOrDefaultWithTenantAsync<User>(
        _tenantContext,
        \"SELECT * FROM Users WHERE Id = @Id AND TenantId = @TenantId\",
        new { Id = id }
    );
}
```

**Benefits:**

- **DRY principle** - Tenant filtering logic in one place
- **Type-safe** - Compile-time checking
- **Testable** - Can mock IDbConnectionFactory
- **Clean Architecture** - Interfaces in Application, implementations in Infrastructure"

---

### Q: "How do you ensure tenant data isolation?"

**A:** "I ensure **complete tenant data isolation** using a multi-layered approach:

**1. TenantContext Service (Request-Scoped)**

```csharp
public interface ITenantContext {
    Guid TenantId { get; }
    bool IsResolved { get; }
}
```

- Stores current tenant ID per HTTP request
- Scoped lifetime = fresh instance per request (thread-safe)

**2. TenantResolutionMiddleware**

- Extracts `X-Tenant-Id` header from HTTP request
- Validates GUID format
- Sets `TenantContext.TenantId` before request reaches controllers
- Returns **400 Bad Request** if header missing/invalid
- Bypasses Swagger/health check endpoints

**3. Repository-Level Filtering**

- Every query includes `WHERE TenantId = @TenantId`
- `BaseRepository` enforces this automatically
- UserRepository inherits tenant filtering

**4. Database Constraints**

- `UNIQUE(TenantId, Email)` - Same email allowed across tenants
- Foreign key: `TenantId REFERENCES Tenants(Id) ON DELETE CASCADE`
- Indexes on TenantId for performance

**Testing proof:**

- Tenant A created user john@tenant1.com
- Tenant B created user jane@tenant2.com
- Tenant A query returns only John ✓
- Tenant B query returns only Jane ✓
- Tenant B trying to GET Tenant A's user → 404 ✓
- Same email (john@tenant1.com) created in both tenants ✓

**Security:** Even if application bug occurs, database constraints prevent cross-tenant access."

---

### Q: "Why did you convert from SERIAL to UUID for IDs?"

**A:** "I migrated from `SERIAL` (auto-increment integers) to `UUID` in migration `0002_ConvertToUUID.sql` because:

**UUID Advantages:**

1. **Globally unique** - No collisions across distributed systems
2. **Security** - Can't guess next ID (integers are predictable: 1, 2, 3...)
3. **Distributed generation** - IDs created in application, not database
4. **Multi-tenant friendly** - No risk of ID collision between tenants
5. **Merging databases** - Can merge tenant databases without ID conflicts

**Trade-offs:**

- **Storage**: 16 bytes vs 4 bytes (acceptable for modern databases)
- **Index size**: Slightly larger B-tree indexes
- **Readability**: Less human-readable than integers

**PostgreSQL implementation:**

```sql
CREATE TABLE Users (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL REFERENCES Tenants(Id),
    ...
);
```

**C# usage:**

```csharp
entity.Id = Guid.NewGuid(); // Generate in application
entity.TenantId = TenantId;  // From TenantContext
```

**Best practice:** UUIDs are industry standard for microservices and multi-tenant SaaS applications."

---

### Q: "Explain the request pipeline for a multi-tenant request."

**A:** "Here's the complete flow for `GET /api/v1/users` with `X-Tenant-Id: aaaaa...`:

**1. HTTP Request arrives**

- Headers: `X-Tenant-Id: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa`

**2. GlobalExceptionHandlerMiddleware**

- Wraps entire pipeline in try-catch
- Catches any unhandled exceptions

**3. TenantResolutionMiddleware**

- Reads `X-Tenant-Id` header
- Validates GUID format
- Calls `tenantContext.SetTenantId(tenantId)`
- If invalid/missing → returns 400 immediately
- If valid → continues to next middleware

**4. Routing**

- Matches `/api/v1/users` to `UsersController.GetAllUsers()`
- API version v1 selected

**5. Controller Execution**

- DI injects `IUserRepository` and `ITenantContext`
- Controller calls `await _userRepository.GetAllAsync()`

**6. Repository Query**

- `UserRepository.GetAllAsync()` inherited from `BaseRepository`
- SQL: `SELECT * FROM Users WHERE TenantId = @TenantId ORDER BY CreatedAt DESC`
- `DapperExtensions.QueryWithTenantAsync()` adds TenantId parameter
- Dapper executes against PostgreSQL

**7. Database Execution**

- Query filtered by TenantId at database level
- Returns only tenant's users (e.g., John Doe)

**8. Response**

- Returns JSON: `{ tenantId: \"aaaa...\", count: 1, users: [...] }`

**9. Cleanup**

- Scoped services (TenantContext, Repository) disposed
- Connection returned to pool

**Key Points:**

- **Request-scoped** = fresh TenantContext per request (no cross-contamination)
- **Early validation** = tenant errors caught before reaching business logic
- **Automatic filtering** = impossible to forget WHERE TenantId clause"

---

### Q: "What are Dapper parameterized queries and why are they important?"

**A:** "Dapper uses **parameterized queries** to prevent SQL injection:

**Bad (vulnerable to SQL injection):**

```csharp
var sql = $\"SELECT * FROM Users WHERE Email = '{email}'\";
// Attacker sends: email = \"'; DROP TABLE Users; --\"
```

**Good (parameterized):**

```csharp
var sql = \"SELECT * FROM Users WHERE Email = @Email AND TenantId = @TenantId\";
var user = await connection.QuerySingleOrDefaultAsync<User>(
    sql,
    new { Email = email, TenantId = tenantId }
);
```

**How it works:**

1. Dapper sends SQL with placeholders (`@Email`, `@TenantId`) to PostgreSQL
2. Sends parameters separately as typed values
3. PostgreSQL treats parameters as **data, not code**
4. Impossible to inject malicious SQL

**Benefits:**

- **Security** - Prevents SQL injection attacks
- **Performance** - Query plans cached by database
- **Type safety** - Parameters are strongly typed

**My implementation:**
All repository queries use parameterized queries via Dapper's anonymous objects or `DynamicParameters`."

---

### Q: "How would you test the multi-tenant isolation?"

**A:** "I tested multi-tenant isolation with these scenarios:

**Unit Tests (to be implemented):**

```csharp
[Fact]
public async Task GetAllAsync_OnlyReturnsTenantData() {
    // Arrange: Mock TenantContext with Tenant A's ID
    // Act: Call repository.GetAllAsync()
    // Assert: Only Tenant A's records returned
}
```

**Integration Tests (to be implemented):**

```csharp
[Fact]
public async Task GetUser_CrossTenantAccess_Returns404() {
    // Create user in Tenant A
    // Try to GET from Tenant B
    // Assert: 404 Not Found
}
```

**Manual testing (already done):**

1. ✅ Created user for Tenant A (john@tenant1.com)
2. ✅ Created user for Tenant B (jane@tenant2.com)
3. ✅ Tenant A GET returns only John
4. ✅ Tenant B GET returns only Jane
5. ✅ Tenant B cannot GET Tenant A's user (404)
6. ✅ Same email allowed in different tenants

**Database-level testing:**

```sql
-- Should return only Tenant A's users
SELECT * FROM Users WHERE TenantId = 'aaaaa...';
```

**Security testing:**

- Try requests without X-Tenant-Id header → 400
- Try invalid GUID → 400
- Try SQL injection in tenant ID → Parameterized queries prevent it

This multi-layered testing ensures tenant isolation at every level."

---

## Day 3: Authentication & Authorization

### Q: "Why did you choose BCrypt for password hashing?"

**A:** "I chose **BCrypt** (BCrypt.Net-Next package) over basic hashing algorithms because:

**Security Features:**

1. **Adaptive hashing** - Cost factor (work factor) can be increased over time as hardware improves
2. **Built-in salt** - Automatically generates random salt per password (prevents rainbow table attacks)
3. **Slow by design** - Computationally expensive to hash (prevents brute-force attacks)
4. **Industry standard** - Proven track record, used by major platforms

**Why NOT SHA256/MD5:**

- SHA256/MD5 are **too fast** - Attacker can try millions of passwords per second
- No built-in salt - Must implement salting manually (error-prone)
- Not designed for passwords - Designed for file integrity, not authentication

**Implementation:**

```csharp
public class PasswordHasher : IPasswordHasher {
    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
```

**Security benefits:**

- Even if database is compromised, passwords remain protected
- Each password has unique salt (same password = different hash)
- Cost factor = 10 (adjustable for future hardware improvements)"

---

### Q: "Explain your JWT token implementation."

**A:** "I implemented **JWT (JSON Web Tokens)** for stateless authentication:

**Token Structure:**

```
Header.Payload.Signature
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
eyJ1c2VySWQiOiIxMjMiLCJlbWFpbCI6ImpvaG5AZXhhbXBsZS5jb20ifQ.
SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

**Claims stored in token:**

```csharp
new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
new Claim(ClaimTypes.Email, email),
new Claim(ClaimTypes.Role, \"TenantAdmin\"),
new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
```

**Configuration (appsettings.json):**

```json
{
  \"Jwt\": {
    \"SecretKey\": \"MyDevelopmentSecretKeyForJWTTokenGeneration12345\",
    \"Issuer\": \"BookingSystemAPI\",
    \"Audience\": \"BookingSystemClient\",
    \"ExpirationMinutes\": 60
  }
}
```

**Token Validation:**

```csharp
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuer = true,           // Check token was issued by our API
    ValidateAudience = true,         // Check token is for our client
    ValidateLifetime = true,         // Check token not expired
    ValidateIssuerSigningKey = true, // Verify signature
    ClockSkew = TimeSpan.Zero        // No tolerance for expired tokens
};
```

**Why JWT over sessions:**

- **Stateless** - No server-side session storage needed (scales horizontally)
- **Self-contained** - All user info in token (no database lookup per request)
- **Cross-domain** - Works across microservices without shared session store
- **Mobile-friendly** - Perfect for mobile apps and SPAs

**Security:**

- Signed with HMAC SHA256 (tamper-proof)
- 60-minute expiration (limits damage if stolen)
- Secret key from configuration (not hardcoded)
- In production, would use Azure Key Vault for secret"

---

### Q: "Why did you set ClockSkew to TimeSpan.Zero?"

**A:** "By default, ASP.NET Core JWT middleware has a **5-minute clock skew tolerance** to account for time differences between servers.

**Default behavior:**

- Token expires at 10:00:00
- Token still accepted until 10:05:00 (5 minutes grace period)

**Why I set ClockSkew = TimeSpan.Zero:**

1. **Stricter security** - Tokens expire exactly when they should
2. **Docker/Cloud environments** - Time sync is reliable (NTP), no need for tolerance
3. **Predictable behavior** - Expiration time means expiration time
4. **Better for testing** - No surprises with \"expired but still works\"

**Configuration:**

```csharp
ClockSkew = TimeSpan.Zero // Removes default 5-minute tolerance
```

**Trade-off:**

- Risk: If servers have time drift, valid tokens might be rejected
- Mitigation: Use NTP synchronization (standard in production)

For this project, strict expiration is worth it for security."

---

### Q: "Explain the difference between RegisterTenant and RegisterUser commands."

**A:** "These are two separate CQRS commands with different semantics:

**RegisterTenant Command:**

- **Purpose**: Create a new organization (tenant) + first admin user
- **Who**: Public endpoint (anyone can register)
- **Requires**: TenantName, Email, Password, FirstName, LastName, Plan
- **Tenant Context**: NOT required (creates new tenant)
- **Process**:
  1. Check if tenant email exists
  2. Create Tenant entity
  3. Hash password
  4. Create User with **direct SQL** (bypasses tenant context)
  5. Assign **TenantAdmin** role
  6. Generate JWT token
  7. Return AuthResult

**RegisterUser Command:**

- **Purpose**: Add user to existing tenant
- **Who**: Authenticated admin or public (depends on authorization)
- **Requires**: Email, Password, FirstName, LastName, Roles
- **Tenant Context**: REQUIRED (X-Tenant-Id header)
- **Process**:
  1. Validate tenant context is set
  2. Check if user email exists in tenant
  3. Hash password
  4. Create User via **UserRepository** (uses tenant context)
  5. Assign specified roles
  6. Generate JWT token
  7. Return AuthResult

**Key Differences:**

| Aspect               | RegisterTenant          | RegisterUser                  |
| -------------------- | ----------------------- | ----------------------------- |
| **Creates**          | Tenant + User           | User only                     |
| **X-Tenant-Id**      | Not required            | Required                      |
| **User Creation**    | Direct SQL (no context) | Via Repository (with context) |
| **Default Role**     | TenantAdmin             | User                          |
| **Email Uniqueness** | Global                  | Per-tenant                    |

**Why direct SQL for RegisterTenant?**

Because TenantContext isn't set yet (we're creating the tenant), so UserRepository's automatic tenant filtering would fail. Direct SQL bypasses this."

---

### Q: "How does the Login flow work?"

**A:** "The login flow uses email as the identifier for BOTH tenant and user:

**Step-by-step process:**

1. **Client sends:** `POST /api/v1/auth/login` with `{ email, password }`

2. **Find Tenant by Email:**

   ```csharp
   var tenant = await _tenantRepository.GetByEmailAsync(request.Email);
   ```

   - Assumption: Tenant was registered with same email as admin user
   - If tenant not found → \"Invalid email or password\" (generic error)

3. **Find User within Tenant:**

   ```csharp
   var user = await connection.QuerySingleOrDefaultAsync<dynamic>(
       \"SELECT * FROM Users WHERE Email = @Email AND TenantId = @TenantId\",
       new { Email = request.Email, TenantId = tenant.Id }
   );
   ```

   - If user not found → \"Invalid email or password\"

4. **Check if User is Active:**

   ```csharp
   if (!user.IsActive) {
       throw new UnauthorizedAccessException(\"User account is inactive\");
   }
   ```

5. **Verify Password:**

   ```csharp
   var passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
   ```

   - BCrypt constant-time comparison (prevents timing attacks)

6. **Get User Roles:**

   ```csharp
   SELECT r.Name FROM UserRoles ur
   INNER JOIN Roles r ON ur.RoleId = r.Id
   WHERE ur.UserId = @UserId AND ur.TenantId = @TenantId
   ```

7. **Generate JWT Token:**

   ```csharp
   var token = _jwtTokenService.GenerateToken(user.Id, user.Email, roles);
   ```

8. **Return AuthResult:**
   ```json
   {
     \"authResult\": {
       \"token\": \"eyJhbGci...\",
       \"userId\": \"123...\",
       \"email\": \"john@example.com\",
       \"roles\": [\"TenantAdmin\"],
       \"tenantId\": \"abc...\",
       \"tenantName\": \"Acme Corp\"
     },
     \"message\": \"Login successful\"
   }
   ```

**Security Features:**

- **Generic error messages** - \"Invalid email or password\" prevents user enumeration
- **Constant-time password verification** - Prevents timing attacks
- **Active check** - Supports account suspension
- **Tenant isolation** - User lookup within specific tenant only
- **No X-Tenant-Id required** - Email automatically resolves tenant"

---

### Q: "Explain your authorization policies."

**A:** "I implemented **role-based authorization** with a default FallbackPolicy:

**1. FallbackPolicy (Global Default):**

```csharp
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
```

- **Applies to all endpoints by default**
- Requires valid JWT token
- Controllers/endpoints must explicitly opt-out with `[AllowAnonymous]`

**2. Named Role-Based Policies:**

```csharp
options.AddPolicy(\"AdminOnly\", policy =>
    policy.RequireRole(\"TenantAdmin\"));

options.AddPolicy(\"ManagerOrAdmin\", policy =>
    policy.RequireRole(\"TenantAdmin\", \"Manager\"));

options.AddPolicy(\"AllUsers\", policy =>
    policy.RequireRole(\"TenantAdmin\", \"Manager\", \"User\"));
```

**3. Controller-Level Protection:**

```csharp
[ApiController]
[Authorize]  // Requires authentication (from FallbackPolicy)
public class UsersController : ControllerBase { ... }
```

**4. Public Endpoints:**

```csharp
[ApiController]
[AllowAnonymous]  // Bypasses FallbackPolicy
public class AuthController : ControllerBase { ... }
```

**Usage Examples:**

```csharp
// Only TenantAdmin can delete users
[HttpDelete(\"{id}\")]
[Authorize(Policy = \"AdminOnly\")]
public async Task<IActionResult> DeleteUser(Guid id) { ... }

// Managers and Admins can approve bookings
[HttpPost(\"approve\")]
[Authorize(Policy = \"ManagerOrAdmin\")]
public async Task<IActionResult> ApproveBooking() { ... }

// All authenticated users can view their profile
[HttpGet(\"my-profile\")]
[Authorize(Policy = \"AllUsers\")]
public async Task<IActionResult> GetMyProfile() { ... }
```

**Why FallbackPolicy?**

- **Secure by default** - Forget to add [Authorize]? Still protected
- **Explicit opt-out** - Must consciously allow anonymous access
- **Prevents mistakes** - Can't accidentally expose sensitive endpoints

**Roles in JWT:**

```csharp
new Claim(ClaimTypes.Role, \"TenantAdmin\")
```

Middleware automatically populates `User.IsInRole(\"TenantAdmin\")` from token claims."

---

### Q: "How do you prevent security vulnerabilities in authentication?"

**A:** "I implemented multiple security layers:

**1. Password Security:**

- **BCrypt hashing** with built-in salt (prevents rainbow tables)
- **Password complexity validation**:
  - Minimum 8 characters
  - At least 1 uppercase
  - At least 1 lowercase
  - At least 1 number
- **No password in logs** - Only hashed values stored

**2. SQL Injection Prevention:**

- **Parameterized queries** everywhere:
  ```csharp
  \"SELECT * FROM Users WHERE Email = @Email\"  // Safe
  ```
- Dapper automatically escapes parameters

**3. Authentication Security:**

- **Generic error messages**: \"Invalid email or password\"
  - Prevents username enumeration attacks
  - Attacker can't tell if email exists
- **Constant-time password comparison** (BCrypt.Verify)
  - Prevents timing attacks
- **Account status check**: Reject inactive users

**4. JWT Security:**

- **HMAC SHA256 signature** - Prevents token tampering
- **Short expiration** - 60 minutes (limits damage if stolen)
- **Token validation** on every request:
  - ValidateIssuer, ValidateAudience, ValidateLifetime
  - ValidateIssuerSigningKey
- **No sensitive data in token** - Only userId, email, roles
- **JTI claim** - Unique token ID (supports revocation if needed)

**5. Authorization Security:**

- **FallbackPolicy** - All endpoints protected by default
- **Role-based access** - TenantAdmin, Manager, User roles
- **[AllowAnonymous]** - Explicit for public endpoints only

**6. Configuration Security:**

- **Secret key externalized** - appsettings, not hardcoded
- **Different keys** for Dev vs Production
- **Environment variables** in production
- **Production placeholder** - Forces explicit configuration

**7. Multi-Tenant Security:**

- **Tenant isolation** in user lookup
- **Email uniqueness per tenant** - Not global
- **TenantId in token** - For audit logging

**8. HTTPS Enforcement:**

- `app.UseHttpsRedirection()` - All traffic encrypted
- Tokens never sent over HTTP

**What I'd add for production:**

- **Rate limiting** - Prevent brute-force login attempts
- **Account lockout** - After N failed attempts
- **2FA/MFA** - Time-based OTP for sensitive operations
- **Refresh tokens** - Long-lived refresh, short-lived access
- **Token revocation** - Blacklist stolen tokens
- **Audit logging** - Track all login attempts
- **CORS** - Restrict API access to known domains"

---

### Q: "Why use CQRS for authentication commands?"

**A:** "I used **CQRS (Command Query Responsibility Segregation)** with MediatR for authentication to:

**Benefits:**

1. **Separation of Concerns:**

   ```
   Controller → Command → Handler → Services/Repositories
   ```

   - Controller: HTTP layer (minimal logic)
   - Command: Data container (what to do)
   - Handler: Business logic (how to do it)

2. **Reusability:**
   - Same RegisterTenantCommand can be triggered from:
     - API endpoint
     - Background job
     - Admin console
     - Migration script

3. **Validation Pipeline:**

   ```
   Request → MediatR → FluentValidation → Handler
   ```

   - Validators run automatically before handler
   - Fail fast if invalid (no database calls)
   - Centralized validation logic

4. **Testability:**
   - Test handlers in isolation
   - Mock repositories easily
   - No HTTP context required

5. **Middleware Support:**
   - MediatR pipeline behaviors:
     - Logging (before/after every command)
     - Performance monitoring
     - Transaction management
     - Caching

**Example Flow:**

```csharp
// 1. Controller (thin orchestrator)
[HttpPost(\"register-tenant\")]
public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request) {
    var command = new RegisterTenantCommand { ... };
    var response = await _mediator.Send(command);
    return CreatedAtAction(nameof(RegisterTenant), response);
}

// 2. Command (data container)
public class RegisterTenantCommand : IRequest<RegisterTenantResponse> {
    public string Email { get; set; }
    public string Password { get; set; }
}

// 3. Validator (runs automatically)
public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand> {
    public RegisterTenantCommandValidator() {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

// 4. Handler (business logic)
public class RegisterTenantCommandHandler : IRequestHandler<...> {
    public async Task<RegisterTenantResponse> Handle(...) {
        // Create tenant, user, assign roles, generate token
    }
}
```

**Alternative (without CQRS):**

- All logic in controller → Fat controllers (anti-pattern)
- Hard to test (requires HTTP context)
- No reusability
- Validation scattered everywhere

**CQRS keeps controllers thin and handlers focused on one responsibility.**"

---

### Q: "How would you implement refresh tokens?"

**A:** "Currently using only **access tokens** (60-minute expiration). For production, I'd add **refresh tokens**:

**Current flow:**

1. Login → Get access token (60min)
2. Token expires → Must login again

**Refresh token flow:**

1. Login → Get access token (15min) + refresh token (7 days)
2. Access expires → Use refresh token to get new access token
3. Refresh expires → Must login again

**Implementation:**

**1. Database Table:**

```sql
CREATE TABLE RefreshTokens (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES Users(Id),
    Token VARCHAR(500) NOT NULL UNIQUE,
    ExpiresAt TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    RevokedAt TIMESTAMP NULL,
    ReplacedByToken VARCHAR(500) NULL
);
```

**2. Generate Refresh Token:**

```csharp
public class JwtTokenService : IJwtTokenService {
    public string GenerateRefreshToken() {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
```

**3. Store After Login:**

```csharp
var refreshToken = _jwtTokenService.GenerateRefreshToken();
await _refreshTokenRepository.AddAsync(new RefreshToken {
    UserId = user.Id,
    Token = refreshToken,
    ExpiresAt = DateTime.UtcNow.AddDays(7)
});
```

**4. New Endpoint:**

```csharp
[HttpPost(\"refresh\")]
[AllowAnonymous]
public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request) {
    // 1. Validate refresh token exists and not expired
    var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
    if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow) {
        return Unauthorized(\"Invalid refresh token\");
    }

    // 2. Generate new access token
    var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
    var newAccessToken = _jwtTokenService.GenerateToken(user.Id, user.Email, roles);

    // 3. Optionally rotate refresh token
    var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
    refreshToken.RevokedAt = DateTime.UtcNow;
    refreshToken.ReplacedByToken = newRefreshToken;
    await _refreshTokenRepository.UpdateAsync(refreshToken);

    return Ok(new {
        AccessToken = newAccessToken,
        RefreshToken = newRefreshToken
    });
}
```

**Security:**

- **Longer refresh expiration** (7 days vs 15 min access)
- **Rotation** - Replace refresh token on each use (prevents reuse)
- **Revocation** - Can invalidate all refresh tokens for user
- **Secure storage** - Refresh tokens in HttpOnly cookies (XSS protection)

**Benefits:**

- **Better UX** - Users stay logged in longer
- **Security** - Short access token limits damage if stolen
- **Revocable** - Can force logout by invalidating refresh tokens

**Current decision:** Kept it simple with 60-min access tokens for MVP. Would add refresh tokens for production."

---

### Q: "What's the difference between [Authorize] and [AllowAnonymous]?"

**A:** "`[Authorize]` and `[AllowAnonymous]` control access to endpoints:

**[Authorize]:**

- **Requires authentication** - Must send valid JWT token
- **Returns 401 Unauthorized** if no token or invalid token
- **Applied at**:
  - Controller level (all actions require auth)
  - Action level (specific endpoint requires auth)

**[AllowAnonymous]:**

- **Bypasses authentication** - No JWT token required
- **Overrides [Authorize]** at controller level
- **For public endpoints** like login, register

**Examples:**

```csharp
// All actions require authentication
[Authorize]
public class UsersController : ControllerBase {
    [HttpGet] // Requires auth
    public async Task<IActionResult> GetAll() { ... }

    [HttpGet(\"{id}\")] // Requires auth
    public async Task<IActionResult> GetById(Guid id) { ... }
}

// All actions public by default
[AllowAnonymous]
public class AuthController : ControllerBase {
    [HttpPost(\"login\")] // Public
    public async Task<IActionResult> Login() { ... }

    [HttpPost(\"register-tenant\")] // Public
    public async Task<IActionResult> RegisterTenant() { ... }
}

// Mixed access
[Authorize]
public class ProductsController : ControllerBase {
    [AllowAnonymous] // Public - anyone can view
    [HttpGet]
    public async Task<IActionResult> GetAll() { ... }

    [HttpPost] // Protected - only authenticated users can create
    public async Task<IActionResult> Create() { ... }
}
```

**FallbackPolicy interaction:**

```csharp
// With FallbackPolicy, ALL endpoints require auth by default
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

// Must explicitly allow anonymous access
[AllowAnonymous]
public class AuthController { ... }

// Without [AllowAnonymous], even endpoints without [Authorize] are protected
```

**My implementation:**

- **FallbackPolicy** = Require auth by default (secure by default)
- **AuthController** = `[AllowAnonymous]` (public registration/login)
- **UsersController, TenantController** = `[Authorize]` (protected resources)

This prevents accidentally exposing protected endpoints."

---

## Day 4: Resources CRUD

(To be filled as we progress)

---

## Day 5: Bookings CRUD & Business Logic

(To be filled as we progress)

---

## Day 6: Advanced Features & Audit Logging

(To be filled as we progress)

---

## Day 7: Backend Testing

(To be filled as we progress)

---

## General Architecture Questions

### Q: "How would you scale this application?"

**A:** "To scale this multi-tenant booking system, I'd use:

**Horizontal Scaling:**

1. **Stateless API** - Current design is stateless (no session state), so I can run multiple instances behind a load balancer
2. **Database read replicas** - PostgreSQL supports streaming replication for read-heavy workloads
3. **Caching layer** - Add Redis for:
   - Tenant configuration cache
   - User session data
   - Frequently accessed resources
4. **CDN** - Cache static frontend assets

**Database Optimization:**

1. **Connection pooling** - Npgsql handles this automatically
2. **Indexes** - Already have indexes on TenantId, Email for fast lookups
3. **Partitioning** - Partition Users/Bookings tables by TenantId for large tenants
4. **Separate databases per tenant** - For enterprise customers (tenant-per-database model)

**Microservices (if needed):**

- Split into: Auth Service, Booking Service, Notification Service
- Each with its own database (database-per-service pattern)
- Event-driven communication using RabbitMQ or Kafka

**Current architecture supports scaling** because:

- Clean separation of concerns
- Stateless design
- Repository pattern allows swapping data access
- Docker containerization ready for Kubernetes"

---

### Q: "What would you improve in this architecture?"

**A:** "**Implemented:**

- ✅ Multi-tenancy with data isolation
- ✅ Clean Architecture for maintainability
- ✅ Repository pattern with Dapper
- ✅ Global error handling
- ✅ API versioning
- ✅ Database migrations

**TODO/Future Improvements:**

1. **Caching** - Add Redis for tenant config, reducing database calls
2. **Rate Limiting** - Prevent abuse (planned for Day 2)
3. **CQRS optimization** - Separate read/write data stores for complex queries
4. **Event Sourcing** - For audit trail of all booking changes
5. **Background jobs** - Hangfire for scheduled tasks (cleanup, reminders)
6. **Health checks** - `/health` endpoint for monitoring
7. **Metrics** - Prometheus metrics for observability
8. **Feature flags** - LaunchDarkly for gradual rollouts
9. **API Gateway** - Kong or Ocelot if moving to microservices

**Current state** is production-ready for MVP, these are optimizations for scale."

---

### Q: "Explain your testing strategy."

**A:** "(To be filled on Day 7 when implementing tests)

**Planned approach:**

**Unit Tests:**

- Test business logic in isolation (handlers, validators)
- Mock repositories with Moq
- FluentAssertions for readable assertions
- Target: 80%+ code coverage

**Integration Tests:**

- Test API endpoints end-to-end
- WebApplicationFactory with test database (Testcontainers)
- Verify multi-tenant isolation
- Test authentication flows

**Architecture Tests:**

- Enforce Clean Architecture rules
- Verify dependency directions
- Check naming conventions

Will elaborate after implementation."

---

## Day 4: Resources CRUD & Multi-Tenant Isolation

### Q: "Walk me through how CQRS is implemented for Resources. Why separate commands and queries?"

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

### Q: "How does BaseRepository<T> automatically filter by TenantId? What happens when GetAllAsync() is called?"

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

### Q: "Why add GetPagedAsync to the base IRepository<T> interface instead of just ResourceRepository?"

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

### Q: "Where in the request pipeline does FluentValidation execute? Why not validate inside the CommandHandler?"

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

### Q: "Explain the middleware pipeline order. Why did /auth endpoints get blocked even with [AllowAnonymous]?"

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

### Q: "Explain the database indexes on the Resources table. Why these specific indexes?"

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

### Q: "Why does DELETE use the AdminOnly policy? How is role-based authorization configured?"

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

### Q: "You mentioned 'soft delete' in the code (IsActive column). What's the difference from hard delete?"

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

### Q: "How would you test the multi-tenant isolation? Walk me through your verification approach."

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

## Technical Deep Dives

(Add specific technical questions as they come up during development)
````
