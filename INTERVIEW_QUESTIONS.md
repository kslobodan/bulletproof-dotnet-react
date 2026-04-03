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

(To be filled as we progress)

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

## Technical Deep Dives

(Add specific technical questions as they come up during development)
