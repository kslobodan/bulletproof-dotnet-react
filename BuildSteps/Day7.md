# Day 7: Advanced Backend Features

---

## Admin Audit Endpoints - Step 1: Create Endpoints to View Audit Logs

1. Created `GetPaginatedAuditLogsQuery` in `Application/Features/AuditLogs/Queries/GetPaginatedAuditLogs`
2. Created `GetPaginatedAuditLogsQueryHandler`
3. Created `GetPaginatedAuditLogsQueryValidator`
4. Updated `IAuditLogRepository` interface to add `GetPagedAsync` method
5. Updated `AuditLogRepository` implementation with pagination and filtering
6. Created `AuditLogsController` in `API/Controllers/v1`
7. Build and verify: `dotnet build` - successful ✅

## Soft Delete Pattern - Step 2: Implement Soft Delete for Entities

8. Updated domain entities with soft delete properties:
   - **Resource.cs**: Added `IsDeleted` (default false) and `DeletedAt` properties
   - **Booking.cs**: Added `IsDeleted` and `DeletedAt` properties
   - **AvailabilityRule.cs**: Added `IsDeleted` and `DeletedAt` properties

9. Created migration `0006_AddSoftDeleteSupport.sql` in `Infrastructure/Data/Scripts`:
   - Added `IsDeleted BOOLEAN NOT NULL DEFAULT FALSE`
   - Added `DeletedAt TIMESTAMP NULL`
   - Added index `IX_{Table}_IsDeleted` for each table

10. Updated `IRepository<T>` interface in `Application/Common/Interfaces`:

- Added `Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);`

11. Updated `BaseRepository` in `Infrastructure/Repositories/BaseRepository`:

- Implemented `SoftDeleteAsync` method with UPDATE query

12. Updated `BookingRepository`:

- Added `WHERE IsDeleted = FALSE` to all query methods
- Ensured soft-deleted bookings are excluded from conflict detection

13. Updated `AvailabilityRuleRepository`:

- Added `WHERE IsDeleted = FALSE` to all query methods

14. Updated Delete command handlers to use `SoftDeleteAsync`:

- **DeleteResourceCommandHandler**: Changed from `DeleteAsync` to `SoftDeleteAsync`
- **DeleteBookingCommandHandler**: Changed from `DeleteAsync` to `SoftDeleteAsync`
- **DeleteAvailabilityRuleCommandHandler**: Changed from `DeleteAsync` to `SoftDeleteAsync`

15. Updated `TenantRepository`:

- Added `WHERE IsDeleted = FALSE` filter to prevent showing deleted records

16. Migration execution:
    - Started API: `cd src/BookingSystem.API; dotnet run`
    - DbUp applied migration `0006_AddSoftDeleteSupport.sql` ✅

## Advanced Queries - Step 3: Add Filtering and Sorting to Booking Queries

17. Updated `GetAllBookingsQuery` in `Application/Features/Bookings/Queries/GetAllBookings`:
    - Added `BookingStatus? Status` filter property
    - Added `Guid? ResourceId` filter property
    - Added `Guid? UserId` filter property
    - Added `DateTime? StartDate` filter property
    - Added `DateTime? EndDate` filter property
    - Added `string? SortBy` property (default: "StartTime")
    - Added `string? SortOrder` property (default: "asc")

18. Updated `IBookingRepository` interface in `Application/Common/Interfaces`:
    - Updated `GetPagedAsync` signature to accept filter parameters

19. Implemented dynamic filtering in `GetPagedAsync` in `BookingRepository.cs`:

    ```csharp
    // Build dynamic WHERE clause
    var whereClause = "WHERE b.TenantId = @TenantId AND b.IsDeleted = FALSE";
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

    // Dynamic ORDER BY
    var orderBy = sortBy?.ToLower() switch
    {
        "title" => "b.Title",
        "resource" => "r.Name",
        "status" => "b.Status",
        _ => "b.StartTime"
    };
    var order = sortOrder?.ToLower() == "desc" ? "DESC" : "ASC";
    ```

20. Updated `GetAllBookingsQueryHandler`:
    - Passed filter parameters to repository

21. Created `GetAllBookingsQueryValidator.cs`:
    - Validated `PageNumber >= 1`
    - Validated `PageSize >= 1 AND PageSize <= 100`
    - Validated `SortBy` is one of: "StartTime", "Title", "Resource", "Status"
    - Validated `SortOrder` is one of: "asc", "desc"
    - Validated `StartDate < EndDate` if both provided

## Statistics Endpoints - Step 4: Create Booking Statistics

22. Created `BookingStatisticsDto` in `Application/Features/Bookings/DTOs/`:

    ```csharp
    public record BookingStatisticsDto
    {
        public int TotalBookings { get; init; }
        public int PendingBookings { get; init; }
        public int ConfirmedBookings { get; init; }
        public int CompletedBookings { get; init; }
        public int CancelledBookings { get; init; }
        public Dictionary<string, int> BookingsByResource { get; init; }
        public Dictionary<string, int> BookingsByDay { get; init; }
    }
    ```

23. Created `GetBookingStatisticsQuery` in `Application/Features/Bookings/Queries/GetBookingStatistics`:
    - Added `DateTime? StartDate` and `DateTime? EndDate` for date range filtering

24. Created `GetBookingStatisticsQueryValidator`:
    - Validated `StartDate < EndDate` if both provided

25. Updated `IBookingRepository` interface:
    - Added `Task<BookingStatisticsDto> GetStatisticsAsync(Guid tenantId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken);`

26. Created `GetBookingStatisticsQueryHandler`:
    - Called repository method and returned statistics

27. Implemented `GetStatisticsAsync` in `BookingRepository.cs`:

    ```sql
    -- Count by status using FILTER clause
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
        AND (@EndDate IS NULL OR EndTime <= @EndDate);

    -- Group by resource
    SELECT r.Name, COUNT(*)
    FROM Bookings b
    JOIN Resources r ON b.ResourceId = r.Id
    WHERE b.TenantId = @TenantId AND b.IsDeleted = FALSE
    GROUP BY r.Name;

    -- Group by day using DATE_TRUNC
    SELECT DATE_TRUNC('day', StartTime) as Day, COUNT(*)
    FROM Bookings
    WHERE TenantId = @TenantId AND IsDeleted = FALSE
    GROUP BY DATE_TRUNC('day', StartTime)
    ORDER BY Day DESC
    LIMIT 30;
    ```

28. Updated `BookingsController`:
    - Added `[HttpGet("statistics")]` endpoint
    - Authorized with `[Authorize]` (any authenticated user can view their tenant's statistics)

## Rate Limiting - Step 5: Add Rate Limiting Middleware

29. Installed AspNetCoreRateLimit package in API project:

    ```powershell
    cd src/BookingSystem.API
    dotnet add package AspNetCoreRateLimit
    ```

30. Configured rate limiting in `appsettings.json`:

    ```json
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
    ```

31. Updated `Program.cs` imports:
    - Added `using AspNetCoreRateLimit;`

32. Registered rate limiting services in `Program.cs`:

    ```csharp
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    ```

33. Added rate limiting middleware to pipeline in `Program.cs`:
    ```csharp
    app.UseIpRateLimiting(); // Before UseAuthentication()
    ```

## Refresh Token Security - Step 6: Implement RefreshToken Mechanism

34. Created `RefreshToken` entity in `Domain/Entities`:
    - Properties: `Id`, `Token`, `UserId`, `TenantId`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByToken`, `IsExpired`, `IsRevoked`, `IsActive`

35. Updated `AuthResult` DTO to include `RefreshToken` property

36. Created RefreshToken DTOs in `Application/Features/Authentication/DTOs`:
    - `RefreshTokenRequest` (contains `RefreshToken` string)
    - `RefreshTokenResponse` (contains new `Token` and `RefreshToken`)

37. Created `RefreshAccessTokenCommand` in `Application/Features/Authentication/Commands/RefreshToken`:
    - Command, Handler, Validator (validates token is not empty)

38. Created `IRefreshTokenRepository` in `Application/Common/Interfaces`:
    - `Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)`
    - `Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)`
    - `Task RevokeAsync(Guid id, string replacedByToken, CancellationToken cancellationToken)`

39. Updated `IJwtTokenService` interface:
    - Added `string GenerateRefreshToken()`
    - Kept existing `string GenerateJwtToken(Guid userId, Guid tenantId, string email, IEnumerable<string> roles)`

40. Updated `JwtTokenService` implementation:

    ```csharp
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
    ```

41. Implemented `RefreshTokenRepository` in `Infrastructure/Repositories`:
    - Used Dapper for CRUD operations
    - Implemented token rotation logic (revoke old token when new one is generated)

42. Updated `LoginCommandHandler` to generate and store refresh token:

    ```csharp
    var refreshToken = _jwtTokenService.GenerateRefreshToken();
    var refreshTokenEntity = new RefreshToken
    {
        Token = refreshToken,
        UserId = user.Id,
        TenantId = user.TenantId,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };
    await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

    return new AuthResult
    {
        Token = jwtToken,
        RefreshToken = refreshToken,
        // ... other properties
    };
    ```

43. Implemented `RefreshAccessTokenCommandHandler`:
    - Validate refresh token exists and is active
    - Generate new JWT and refresh token
    - Revoke old refresh token (set `RevokedAt`, `ReplacedByToken`)
    - Store new refresh token
    - Return new tokens

44. Added refresh endpoint to `AuthController`:

    ```csharp
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshAccessTokenCommand { RefreshToken = request.RefreshToken };
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    ```

45. Created database migration `0007_CreateRefreshTokensTable.sql`:

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

46. Registered `IRefreshTokenRepository` in DI container (`Program.cs`):

    ```csharp
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    ```

47. Migration execution:
    - Started API → DbUp applied migration `0007_CreateRefreshTokensTable.sql` ✅

48. Tested refresh token flow:
    - Login: Received access token + refresh token ✅
    - Refresh: Used refresh token → Got new access token + new refresh token ✅
    - Token rotation: Old refresh token marked as revoked in database, `ReplacedByToken` set ✅
    - Security: Old refresh token rejected on reuse attempt ✅
