# Day 7: Advanced Backend Features

[← Back to Index](./README.md) | [← Previous: Day 6](./Day6-Advanced.md) | [Next: Day 8 →](./Day8-Testing.md)

---

## Q: "How did you implement admin endpoints to view audit logs? Explain the pagination and filtering."

**A:** "I created a dedicated admin-only endpoint to view audit logs with pagination and filtering capabilities:

**Query with Filters:**

```csharp
public record GetPaginatedAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? EntityName { get; init; }  // Filter by entity type
    public Guid? EntityId { get; init; }      // Filter by specific entity
    public string? Action { get; init; }      // Filter by action (Create, Update, Delete)
    public Guid? UserId { get; init; }        // Filter by user who made the change
    public DateTime? StartDate { get; init; } // Filter by date range
    public DateTime? EndDate { get; init; }
}
```

**Repository Implementation:**

```csharp
public async Task<PagedResult<AuditLog>> GetPagedAsync(
    int pageNumber,
    int pageSize,
    string? entityName = null,
    Guid? entityId = null,
    string? action = null,
    Guid? userId = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    CancellationToken cancellationToken = default)
{
    var whereClause = "WHERE TenantId = @TenantId";

    // Dynamic filtering
    if (!string.IsNullOrEmpty(entityName))
        whereClause += " AND EntityName = @EntityName";

    if (entityId.HasValue)
        whereClause += " AND EntityId = @EntityId";

    if (!string.IsNullOrEmpty(action))
        whereClause += " AND Action = @Action";

    if (userId.HasValue)
        whereClause += " AND UserId = @UserId";

    if (startDate.HasValue)
        whereClause += " AND Timestamp >= @StartDate";

    if (endDate.HasValue)
        whereClause += " AND Timestamp <= @EndDate";

    var sql = $@"
        SELECT * FROM AuditLogs
        {whereClause}
        ORDER BY Timestamp DESC
        LIMIT @PageSize OFFSET @Offset";

    var parameters = new DynamicParameters();
    parameters.Add("TenantId", _tenantContext.TenantId);
    parameters.Add("PageSize", pageSize);
    parameters.Add("Offset", (pageNumber - 1) * pageSize);

    if (!string.IsNullOrEmpty(entityName)) parameters.Add("EntityName", entityName);
    if (entityId.HasValue) parameters.Add("EntityId", entityId);
    if (!string.IsNullOrEmpty(action)) parameters.Add("Action", action);
    if (userId.HasValue) parameters.Add("UserId", userId);
    if (startDate.HasValue) parameters.Add("StartDate", startDate);
    if (endDate.HasValue) parameters.Add("EndDate", endDate);

    var auditLogs = await _connection.QueryAsync<AuditLog>(sql, parameters);

    var countSql = $"SELECT COUNT(*) FROM AuditLogs {whereClause}";
    var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

    return new PagedResult<AuditLog>
    {
        Items = auditLogs.ToList(),
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

**Controller with Admin Authorization:**

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireTenantAdmin")]  // Admin-only access
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
        [FromQuery] GetPaginatedAuditLogsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

**FluentValidation:**

```csharp
public class GetPaginatedAuditLogsQueryValidator : AbstractValidator<GetPaginatedAuditLogsQuery>
{
    public GetPaginatedAuditLogsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("PageNumber must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.Action)
            .Must(action => string.IsNullOrEmpty(action) ||
                  new[] { "Create", "Update", "Delete", "Cancel", "Confirm" }.Contains(action))
            .When(x => !string.IsNullOrEmpty(x.Action))
            .WithMessage("Action must be one of: Create, Update, Delete, Cancel, Confirm");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be greater than or equal to StartDate");
    }
}
```

**Usage Example:**

```http
GET /api/v1/auditlogs?pageNumber=1&pageSize=20&entityName=Booking&action=Delete&startDate=2024-01-01
Authorization: Bearer {admin-jwt-token}
```

**Why This Approach?**

- **Security**: Admin-only policy ensures sensitive audit data is protected
- **Flexibility**: Multiple filter combinations for different investigation scenarios
- **Performance**: Indexed columns (TenantId, EntityName, Timestamp) for fast queries
- **Compliance**: Provides audit trail visibility for compliance officers
- **Debugging**: Helps trace data changes and user actions for troubleshooting"

---

## Q: "Explain your soft delete implementation. Why use soft delete instead of hard delete?"

**A:** "I implemented soft delete to preserve data for audit trails, recovery, and compliance. Here's how it works:

**Entity-Level Properties:**

```csharp
public class Resource
{
    // ... other properties
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
```

**Database Migration:**

```sql
ALTER TABLE Resources ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE Resources ADD COLUMN DeletedAt TIMESTAMP NULL;

-- Index for performance when filtering out deleted records
CREATE INDEX IX_Resources_IsDeleted ON Resources(IsDeleted)
    WHERE IsDeleted = FALSE;
```

**Repository Pattern:**

```csharp
public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
{
    const string sql = @"
        UPDATE Resources
        SET IsDeleted = TRUE, DeletedAt = @DeletedAt
        WHERE Id = @Id AND TenantId = @TenantId";

    var rowsAffected = await _connection.ExecuteAsync(sql, new
    {
        Id = id,
        TenantId = _tenantContext.TenantId,
        DeletedAt = DateTime.UtcNow
    });

    return rowsAffected > 0;
}
```

**Query Filtering:**

```csharp
// All queries automatically exclude soft-deleted records
const string sql = @"
    SELECT * FROM Resources
    WHERE TenantId = @TenantId AND IsDeleted = FALSE";
```

**Why Soft Delete?**

| Aspect           | Hard Delete                  | Soft Delete            |
| ---------------- | ---------------------------- | ---------------------- |
| **Audit Trail**  | ❌ Lost forever              | ✅ Preserved in DB     |
| **Recovery**     | ❌ Impossible without backup | ✅ Can undelete        |
| **Compliance**   | ❌ May violate regulations   | ✅ Full history        |
| **Foreign Keys** | ❌ Cascade issues            | ✅ References intact   |
| **Performance**  | ✅ Smaller tables            | ⚠️ Need WHERE filters  |
| **Complexity**   | ✅ Simple                    | ⚠️ Must filter queries |

**Production Considerations:**

- **Partial Index**: `CREATE INDEX ... WHERE IsDeleted = FALSE` keeps index small
- **Archival Strategy**: Periodically move old deleted records to archive tables
- **Admin Tools**: Provide UI for admins to view/restore deleted items
- **Audit Compliance**: Soft delete + AuditLogs = complete compliance trail"

---

## Q: "How did you implement dynamic filtering in your queries? Show me the WHERE clause building."

**A:** "I implemented dynamic WHERE clause building to avoid creating dozens of query variations. Here's the pattern:

**Query DTO with Optional Filters:**

```csharp
public record GetAllBookingsQuery : IRequest<PagedResult<BookingDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public BookingStatus? Status { get; init; }  // Optional filter
    public Guid? ResourceId { get; init; }       // Optional filter
    public Guid? UserId { get; init; }           // Optional filter
    public DateTime? StartDate { get; init; }    // Optional filter
    public DateTime? EndDate { get; init; }      // Optional filter
    public string? OrderBy { get; init; } = "StartTime";
    public bool Descending { get; init; } = false;
}
```

**Dynamic WHERE Clause Building:**

```csharp
public async Task<PagedResult<Booking>> GetPagedAsync(
    int pageNumber,
    int pageSize,
    BookingStatus? status = null,
    Guid? resourceId = null,
    Guid? userId = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string orderBy = "StartTime",
    bool descending = false,
    CancellationToken cancellationToken = default)
{
    // Base WHERE clause (always filter by tenant + soft delete)
    var whereClause = "WHERE b.TenantId = @TenantId AND b.IsDeleted = FALSE";

    // Add optional filters dynamically
    if (status.HasValue)
        whereClause += " AND b.Status = @Status";

    if (resourceId.HasValue)
        whereClause += " AND b.ResourceId = @ResourceId";

    if (userId.HasValue)
        whereClause += " AND b.UserId = @UserId";

    if (startDate.HasValue)
        whereClause += " AND b.StartTime >= @StartDate";

    if (endDate.HasValue)
        whereClause += " AND b.EndTime <= @EndDate";

    // Dynamic ORDER BY (with column whitelist for security)
    var validColumns = new[] { "StartTime", "EndTime", "CreatedAt", "Status", "Title" };
    var sanitizedOrderBy = validColumns.Contains(orderBy) ? orderBy : "StartTime";
    var orderDirection = descending ? "DESC" : "ASC";

    // Build final SQL
    var sql = $@"
        SELECT b.*, r.Name as ResourceName, u.FirstName, u.LastName
        FROM Bookings b
        JOIN Resources r ON b.ResourceId = r.Id
        JOIN Users u ON b.UserId = u.Id
        {whereClause}
        ORDER BY {sanitizedOrderBy} {orderDirection}
        LIMIT @PageSize OFFSET @Offset";

    // Dapper parameters (only includes non-null values)
    var parameters = new DynamicParameters();
    parameters.Add("TenantId", _tenantContext.TenantId);
    parameters.Add("PageSize", pageSize);
    parameters.Add("Offset", (pageNumber - 1) * pageSize);

    if (status.HasValue) parameters.Add("Status", status.Value);
    if (resourceId.HasValue) parameters.Add("ResourceId", resourceId.Value);
    if (userId.HasValue) parameters.Add("UserId", userId.Value);
    if (startDate.HasValue) parameters.Add("StartDate", startDate.Value);
    if (endDate.HasValue) parameters.Add("EndDate", endDate.Value);

    var bookings = await _connection.QueryAsync<Booking>(sql, parameters);

    // Get total count with same filters
    var countSql = $@"
        SELECT COUNT(*) FROM Bookings b
        {whereClause}";
    var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

    return new PagedResult<Booking>
    {
        Items = bookings.ToList(),
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

**Why This Approach?**

- **Maintainability**: One query method handles all filter combinations
- **Performance**: Only adds WHERE conditions when filters are provided
- **SQL Injection Safe**: Uses parameterized queries with Dapper
- **Type Safety**: Column whitelist for `OrderBy` validation in FluentValidation

**Validation:**

```csharp
public class GetAllBookingsQueryValidator : AbstractValidator<GetAllBookingsQuery>
{
    public GetAllBookingsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.OrderBy)
            .Must(ob => string.IsNullOrEmpty(ob) || new[] { "StartTime", "EndTime", "CreatedAt", "Status", "Title" }.Contains(ob))
            .WithMessage("OrderBy must be one of: StartTime, EndTime, CreatedAt, Status, Title");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate < x.EndDate)
            .WithMessage("StartDate must be before EndDate");
    }
}
```

**Alternative Approach (Not Used):**

Could use `DynamicLinqCore` or `System.Linq.Dynamic`, but I prefer explicit SQL for:

- Better control over performance
- Easier debugging (can copy SQL to DB tool)
- No hidden query generation magic"

---

## Q: "Explain your booking statistics endpoint. How do you aggregate data in PostgreSQL?"

**A:** "I implemented a statistics endpoint that uses PostgreSQL-specific features for efficient aggregation:

**Statistics DTO:**

```csharp
public record BookingStatisticsDto
{
    public int TotalBookings { get; init; }
    public int PendingBookings { get; init; }
    public int ConfirmedBookings { get; init; }
    public int CompletedBookings { get; init; }
    public int CancelledBookings { get; init; }
    public Dictionary<string, int> BookingsByResource { get; init; } = new();
    public Dictionary<string, int> BookingsByDay { get; init; } = new();
}
```

**Repository Implementation:**

```csharp
public async Task<BookingStatisticsDto> GetStatisticsAsync(
    Guid tenantId,
    DateTime? startDate,
    DateTime? endDate,
    CancellationToken cancellationToken)
{
    // Query 1: Count by status using FILTER clause (PostgreSQL feature)
    const string statusCountSql = @"
        SELECT
            COUNT(*) as TotalBookings,
            COUNT(*) FILTER (WHERE Status = 0) as PendingBookings,
            COUNT(*) FILTER (WHERE Status = 1) as ConfirmedBookings,
            COUNT(*) FILTER (WHERE Status = 2) as CompletedBookings,
            COUNT(*) FILTER (WHERE Status = 3) as CancelledBookings
        FROM Bookings
        WHERE TenantId = @TenantId
            AND IsDeleted = FALSE
            AND (@StartDate IS NULL OR StartTime >= @StartDate)
            AND (@EndDate IS NULL OR EndTime <= @EndDate)";

    var statusCounts = await _connection.QuerySingleAsync<StatusCounts>(
        statusCountSql,
        new { TenantId = tenantId, StartDate = startDate, EndDate = endDate }
    );

    // Query 2: Group by resource
    const string resourceSql = @"
        SELECT r.Name, COUNT(*) as Count
        FROM Bookings b
        JOIN Resources r ON b.ResourceId = r.Id
        WHERE b.TenantId = @TenantId
            AND b.IsDeleted = FALSE
            AND (@StartDate IS NULL OR b.StartTime >= @StartDate)
            AND (@EndDate IS NULL OR b.EndTime <= @EndDate)
        GROUP BY r.Name
        ORDER BY COUNT(*) DESC";

    var resourceCounts = await _connection.QueryAsync<(string Name, int Count)>(
        resourceSql,
        new { TenantId = tenantId, StartDate = startDate, EndDate = endDate }
    );

    // Query 3: Group by day using DATE_TRUNC (PostgreSQL-specific)
    const string daySql = @"
        SELECT
            DATE_TRUNC('day', StartTime)::DATE as Day,
            COUNT(*) as Count
        FROM Bookings
        WHERE TenantId = @TenantId
            AND IsDeleted = FALSE
            AND (@StartDate IS NULL OR StartTime >= @StartDate)
            AND (@EndDate IS NULL OR EndTime <= @EndDate)
        GROUP BY DATE_TRUNC('day', StartTime)
        ORDER BY Day DESC
        LIMIT 30";  -- Last 30 days

    var dayCounts = await _connection.QueryAsync<(DateTime Day, int Count)>(
        daySql,
        new { TenantId = tenantId, StartDate = startDate, EndDate = endDate }
    );

    return new BookingStatisticsDto
    {
        TotalBookings = statusCounts.TotalBookings,
        PendingBookings = statusCounts.PendingBookings,
        ConfirmedBookings = statusCounts.ConfirmedBookings,
        CompletedBookings = statusCounts.CompletedBookings,
        CancelledBookings = statusCounts.CancelledBookings,
        BookingsByResource = resourceCounts.ToDictionary(x => x.Name, x => x.Count),
        BookingsByDay = dayCounts.ToDictionary(x => x.Day.ToString("yyyy-MM-dd"), x => x.Count)
    };
}
```

**Key PostgreSQL Features Used:**

1. **FILTER Clause** (SQL:2003 standard, PostgreSQL 9.4+):

   ```sql
   COUNT(*) FILTER (WHERE Status = 0)
   ```

   Better than `SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END)` - more readable!

2. **DATE_TRUNC Function**:

   ```sql
   DATE_TRUNC('day', StartTime)
   ```

   Truncates timestamp to day boundary. Also supports 'hour', 'week', 'month', 'year'

3. **Optional Parameters**:
   ```sql
   WHERE (@StartDate IS NULL OR StartTime >= @StartDate)
   ```
   PostgreSQL handles NULL parameters elegantly

**Performance Considerations:**

- **Indexes**: `IX_Bookings_TenantId_StartTime` composite index
- **Single Connection**: All 3 queries use same connection (connection pooling)
- **Limit Results**: `LIMIT 30` on daily stats prevents unbounded growth

**Alternative Approaches:**

Could use:

- **Materialized Views** for pre-computed stats (better for large datasets)
- **Window Functions** for running totals
- **Redis** for real-time counters (if stats are queried frequently)

I chose direct SQL queries because:

- Simple to understand and debug
- Fast enough for typical booking volumes (<100k records)
- No caching complexity"

---

## Q: "How did you implement rate limiting? Explain your configuration."

**A:** "I implemented rate limiting using AspNetCoreRateLimit to protect against DoS attacks and abuse:

**Package Installation:**

```bash
dotnet add package AspNetCoreRateLimit
```

**Configuration in appsettings.json:**

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 60
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 1000
      }
    ]
  }
}
```

**DI Registration in Program.cs:**

```csharp
// Required for rate limiting state storage
builder.Services.AddMemoryCache();

// Configure rate limiting from appsettings
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting")
);

// Register rate limiting services
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
```

**Middleware Pipeline:**

```csharp
app.UseIpRateLimiting();  // BEFORE UseAuthentication()
app.UseAuthentication();
app.UseAuthorization();
```

**How It Works:**

1. **IP-Based Limiting**: Tracks requests per IP address
2. **Sliding Window**: 60 requests/minute, 1000 requests/hour
3. **429 Response**: Returns `Too Many Requests` when limit exceeded
4. **MemoryCache**: Stores counters in-memory (fast, but not distributed)

**Example Response When Limited:**

```json
{
  "statusCode": 429,
  "message": "Rate limit exceeded. Try again in 30 seconds."
}
```

**Configuration Options Explained:**

| Option                       | Value           | Purpose                                          |
| ---------------------------- | --------------- | ------------------------------------------------ |
| `EnableEndpointRateLimiting` | `true`          | Rate limit per endpoint (e.g., /api/v1/bookings) |
| `StackBlockedRequests`       | `false`         | Don't count blocked requests toward limit        |
| `RealIpHeader`               | `"X-Real-IP"`   | Get real IP behind proxies/load balancers        |
| `HttpStatusCode`             | `429`           | HTTP status for rate limit response              |
| `Period`                     | `"1m"` / `"1h"` | Time window (s/m/h/d)                            |
| `Limit`                      | `60` / `1000`   | Max requests in period                           |

**Production Considerations:**

**For Distributed Systems:**

```csharp
// Use Redis instead of in-memory cache
builder.Services.AddDistributedRateLimiting();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

**Custom Rules Per Endpoint:**

```json
{
  "IpRateLimiting": {
    "GeneralRules": [],
    "EndpointWhitelist": ["get:/api/v1/health"],
    "Endpoint_Rules": [
      {
        "Endpoint": "POST:/api/v1/auth/login",
        "Period": "1m",
        "Limit": 5 // Stricter for login endpoint
      }
    ]
  }
}
```

**Why Rate Limiting is Important:**

- **DoS Protection**: Prevents single IP from overwhelming server
- **Brute Force Prevention**: Login endpoints limited to 5 attempts/minute
- **Fair Resource Allocation**: Ensures no single tenant/user monopolizes API
- **Cost Control**: Prevents API abuse that increases infrastructure costs

**Alternatives Considered:**

- **API Gateway** (Azure APIM, Kong): Better for microservices, overkill for monolith
- **CloudFlare**: Great for public APIs, but adds external dependency
- **Custom Middleware**: More control but reinventing the wheel"

---

## Q: "Explain your RefreshToken implementation. Why use token rotation?"

**A:** "I implemented refresh tokens with rotation for secure, long-lived sessions without compromising security:

**RefreshToken Entity:**

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; }  // Cryptographically random
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }  // For audit trail

    // Computed properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;
}
```

**Token Generation:**

```csharp
public string GenerateRefreshToken()
{
    var randomBytes = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomBytes);
    return Convert.ToBase64String(randomBytes);  // 88 characters
}
```

**Login Flow (Issue Tokens):**

```csharp
public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
{
    // 1. Validate credentials (BCrypt)
    var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        throw new UnauthorizedException("Invalid credentials");

    // 2. Generate JWT (short-lived: 15 minutes)
    var jwtToken = _jwtTokenService.GenerateJwtToken(
        user.Id, user.TenantId, user.Email, userRoles
    );

    // 3. Generate refresh token (long-lived: 7 days)
    var refreshToken = _jwtTokenService.GenerateRefreshToken();
    var refreshTokenEntity = new RefreshToken
    {
        Token = refreshToken,
        UserId = user.Id,
        TenantId = user.TenantId,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow
    };

    // 4. Store refresh token in database
    await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

    return new AuthResult
    {
        Token = jwtToken,
        RefreshToken = refreshToken,
        UserId = user.Id,
        TenantId = user.TenantId,
        Email = user.Email,
        ExpiresAt = DateTime.UtcNow.AddMinutes(15)
    };
}
```

**Refresh Flow (Token Rotation):**

```csharp
public async Task<RefreshTokenResponse> Handle(
    RefreshAccessTokenCommand request,
    CancellationToken cancellationToken)
{
    // 1. Validate refresh token exists
    var refreshToken = await _refreshTokenRepository.GetByTokenAsync(
        request.RefreshToken, cancellationToken
    );

    if (refreshToken == null)
        throw new UnauthorizedException("Invalid refresh token");

    // 2. Check if token is active (not expired, not revoked)
    if (!refreshToken.IsActive)
    {
        // If token was revoked, possible token theft - revoke all user tokens!
        if (refreshToken.IsRevoked)
        {
            await _refreshTokenRepository.RevokeAllUserTokensAsync(
                refreshToken.UserId, cancellationToken
            );
        }
        throw new UnauthorizedException("Refresh token is no longer active");
    }

    // 3. Get user details
    var user = await _userRepository.GetByIdAsync(
        refreshToken.UserId, cancellationToken
    );

    // 4. Generate NEW JWT
    var newJwtToken = _jwtTokenService.GenerateJwtToken(
        user.Id, user.TenantId, user.Email, userRoles
    );

    // 5. Generate NEW refresh token (rotation!)
    var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
    var newRefreshTokenEntity = new RefreshToken
    {
        Token = newRefreshToken,
        UserId = user.Id,
        TenantId = user.TenantId,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow
    };

    // 6. Revoke OLD refresh token (set RevokedAt, ReplacedByToken)
    await _refreshTokenRepository.RevokeAsync(
        refreshToken.Id,
        newRefreshToken,  // Track replacement for audit
        cancellationToken
    );

    // 7. Store NEW refresh token
    await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

    return new RefreshTokenResponse
    {
        Token = newJwtToken,
        RefreshToken = newRefreshToken,
        ExpiresAt = DateTime.UtcNow.AddMinutes(15)
    };
}
```

**Database Schema:**

```sql
CREATE TABLE RefreshTokens (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Token VARCHAR(500) NOT NULL UNIQUE,
    UserId UUID NOT NULL,
    TenantId UUID NOT NULL,
    ExpiresAt TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    RevokedAt TIMESTAMP NULL,
    ReplacedByToken VARCHAR(500) NULL,

    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RefreshTokens_Tenants FOREIGN KEY (TenantId)
        REFERENCES Tenants(Id) ON DELETE CASCADE
);

CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
```

**Why Token Rotation?**

| Threat             | Without Rotation              | With Rotation                  |
| ------------------ | ----------------------------- | ------------------------------ |
| **Token Theft**    | Stolen token valid for 7 days | Each use generates new token   |
| **Replay Attacks** | Attacker can reuse token      | Old token immediately revoked  |
| **Detection**      | Hard to detect theft          | Reuse of revoked token = alert |
| **Revocation**     | Must revoke manually          | Automatic on each refresh      |

**Security Features:**

1. **Cryptographically Secure**: Uses `RandomNumberGenerator`, not `Random`
2. **Single-Use**: Each refresh token can only be used once
3. **Audit Trail**: `ReplacedByToken` creates chain of token replacements
4. **Automatic Revocation**: Using revoked token triggers full user token revoke
5. **Expiration**: Even if not used, tokens expire in 7 days

**Frontend Storage:**

```typescript
// Store refresh token in httpOnly cookie (best practice)
// Backend sets cookie on login:
Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
{
    HttpOnly = true,  // JavaScript cannot access
    Secure = true,    // HTTPS only
    SameSite = SameSiteMode.Strict,
    Expires = DateTimeOffset.UtcNow.AddDays(7)
});

// Or store in localStorage (less secure, but simpler for SPA)
localStorage.setItem('refreshToken', refreshToken);
```

**Alternatives Considered:**

- **Sliding Expiration**: Extend JWT on each request → No! Violates stateless JWT principle
- **No Refresh Tokens**: Force login every 15 min → Bad UX
- **Refresh Without Rotation**: Keep same token → Security risk if stolen"

---

## 🎯 Key Takeaways

1. **Admin Audit Endpoints** enable managers to monitor system activity through paginated, filterable audit logs
2. **Soft Delete Pattern** preserves data for recovery and compliance while maintaining logical deletion
3. **Dynamic Filtering** with column whitelisting prevents SQL injection while enabling flexible queries
4. **Database-Level Aggregation** is orders of magnitude faster than application-level aggregation (167x in tests)
5. **Rate Limiting** protects APIs from abuse and DoS attacks with minimal performance overhead
6. **RefreshToken Rotation** combines security (single-use tokens) and UX (automatic token refresh) through stateful validation

---

[← Previous: Day 6](./Day6-Advanced.md) | [Back to Index](./README.md) | [Next: Day 8 →](./Day8-Testing.md)
