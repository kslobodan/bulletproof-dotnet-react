# Day 7: Advanced Backend Features

---

## Admin Audit Endpoints - Step 1: Create Endpoints to View Audit Logs

1. Created `GetPaginatedAuditLogsQuery` in `Application/Features/AuditLogs/Queries/GetPaginatedAuditLogs`
2. Created `GetPaginatedAuditLogsQueryHandler`
3. Created `GetPaginatedAuditLogsQueryValidator`
4. Updated `IAuditLogRepository` interface to add `GetPagedAsync` method
5. Updated `AuditLogRepository` implementation with pagination and filtering
6. Created `AuditLogsController` in `API/Controllers/v1`

## Soft Delete Pattern - Step 2: Implement Soft Delete for Entities

7. Updated domain entities with soft delete properties:
   - **Resource.cs**: Added `IsDeleted` (default false) and `DeletedAt` properties
   - **Booking.cs**: Added `IsDeleted` and `DeletedAt` properties
   - **AvailabilityRule.cs**: Added `IsDeleted` and `DeletedAt` properties

8. Created migration `0006_AddSoftDeleteSupport.sql` in `Infrastructure/Data/Scripts`:
   - Added `IsDeleted BOOLEAN NOT NULL DEFAULT FALSE`
   - Added `DeletedAt TIMESTAMP NULL`
   - Added index `IX_{Table}_IsDeleted` for each table

9. Updated `IRepository<T>` interface in `Application/Common/Interfaces`:

- Added `Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);`

10. Updated `BaseRepository` in `Infrastructure/Repositories/BaseRepository`:

- Implemented `SoftDeleteAsync` method with UPDATE query

11. Updated `BookingRepository`:

- Added `WHERE IsDeleted = FALSE` to all query methods
- Ensured soft-deleted bookings are excluded from conflict detection

12. Updated `AvailabilityRuleRepository`:

- Added `WHERE IsDeleted = FALSE` to all query methods

13. Updated Delete command handlers to use `SoftDeleteAsync`:

- **DeleteResourceCommandHandler**: Changed from `DeleteAsync` to `SoftDeleteAsync`
- **DeleteBookingCommandHandler**: Changed from `DeleteAsync` to `SoftDeleteAsync`
- **DeleteAvailabilityRuleCommandHandler**: Changed from `DeleteAsync` to `SoftDeleteAsync`

14. Updated `TenantRepository`:

- Added `WHERE IsDeleted = FALSE` filter to prevent showing deleted records

## Advanced Queries - Step 3: Add Filtering and Sorting to Booking Queries

15. Updated `GetAllBookingsQuery` in `Application/Features/Bookings/Queries/GetAllBookings`:
    - Added `BookingStatus? Status` filter property
    - Added `Guid? ResourceId` filter property
    - Added `Guid? UserId` filter property
    - Added `DateTime? StartDate` filter property
    - Added `DateTime? EndDate` filter property
    - Added `string? OrderBy` property (default: "StartTime")
    - Added `bool Descending` property (default: false)

16. Updated `IBookingRepository` interface in `Application/Common/Interfaces`:
    - Updated `GetPagedAsync` signature to accept filter parameters (resourceId, userId, status, startDate, endDate, orderBy, descending)

17. Implemented dynamic filtering in `GetPagedAsync` in `BookingRepository.cs`:
    - Built dynamic WHERE clause based on provided filters
    - Added ORDER BY with sanitized column names (StartTime, EndTime, CreatedAt, Status, Title)
    - Added DESC/ASC order direction based on `descending` parameter

18. Updated `GetAllBookingsQueryHandler`:
    - Passed filter parameters to repository (`request.OrderBy ?? "StartTime"`, `request.Descending`)

19. Created `GetAllBookingsQueryValidator.cs`:
    - Validated `PageNumber >= 1`
    - Validated `PageSize >= 1 AND PageSize <= 100`
    - Validated `OrderBy` is one of: "StartTime", "EndTime", "CreatedAt", "Status", "Title"
    - Validated `EndDate >= StartDate` if both provided

## Statistics Endpoints - Step 4: Create Booking Statistics

20. Created `BookingStatisticsDto` in `Application/Features/Bookings/DTOs/`:
    - Properties: `TotalBookings`, `PendingBookings`, `ConfirmedBookings`, `CompletedBookings`, `CancelledBookings`, `MostBookedResourceId`, `MostBookedResourceName`, `BookingsPerDay` (dictionary)

21. Created `GetBookingStatisticsQuery` in `Application/Features/Bookings/Queries/GetBookingStatistics`:
    - Added `DateTime? StartDate` and `DateTime? EndDate` for date range filtering

22. Created `GetBookingStatisticsQueryValidator`:
    - Validated `StartDate < EndDate` if both provided

23. Updated `IBookingRepository` interface:
    - Added `Task<BookingStatisticsDto> GetStatisticsAsync(Guid tenantId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken);`

24. Created `GetBookingStatisticsQueryHandler`:
    - Called repository method and returned statistics

25. Implemented `GetStatisticsAsync` in `BookingRepository.cs`:
    - Used PostgreSQL aggregation functions: `COUNT(*) FILTER (WHERE Status = ...)`, `GROUP BY`, `DATE_TRUNC('day', StartTime)`
    - Counted bookings by status
    - Found most booked resource with JOIN and COUNT
    - Calculated bookings per day for trend analysis
    - Example query:

26. Updated `BookingsController`:
    - Added `[HttpGet("statistics")]` endpoint
    - Authorized with `[Authorize]` (any authenticated user can view their tenant's statistics)

## Rate Limiting - Step 5: Add Rate Limiting Middleware

27. Installed AspNetCoreRateLimit package in API project - `dotnet add package AspNetCoreRateLimit`

28. Configured rate limiting in `appsettings.json`:

29. Updated `Program.cs` imports:
    - Added `using AspNetCoreRateLimit;`

30. Registered rate limiting services in `Program.cs`:

31. Added rate limiting middleware to pipeline in `Program.cs`:

## Refresh Token Security - Step 6: Implement RefreshToken Mechanism

32. Created `RefreshToken` entity in `Domain/Entities`:
    - Properties: `Id`, `Token`, `UserId`, `TenantId`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByToken`, `IsExpired`, `IsRevoked`, `IsActive`

33. Updated `AuthResult` DTO to include `RefreshToken` property

34. Created RefreshToken DTOs in `Application/Features/Authentication/DTOs`:
    - `RefreshTokenRequest` (contains `RefreshToken` string)
    - `RefreshTokenResponse` (contains new `Token` and `RefreshToken`)

35. Created `RefreshAccessTokenCommand` in `Application/Features/Authentication/Commands/RefreshToken`:
    - Command, Handler, Validator (validates token is not empty)

36. Created `IRefreshTokenRepository` in `Application/Common/Interfaces`:
    - `Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)`
    - `Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)`
    - `Task RevokeAsync(Guid id, string replacedByToken, CancellationToken cancellationToken)`

37. Updated `IJwtTokenService` interface:
    - Added `string GenerateRefreshToken()`
    - Kept existing `string GenerateJwtToken(Guid userId, Guid tenantId, string email, IEnumerable<string> roles)`

38. Updated `JwtTokenService` implementation:

39. Implemented `RefreshTokenRepository` in `Infrastructure/Repositories`:
    - Used Dapper for CRUD operations
    - Implemented token rotation logic (revoke old token when new one is generated)

40. Updated `LoginCommandHandler` to generate and store refresh token:

41. Implemented `RefreshAccessTokenCommandHandler`:
    - Validate refresh token exists and is active
    - Generate new JWT and refresh token
    - Revoke old refresh token (set `RevokedAt`, `ReplacedByToken`)
    - Store new refresh token
    - Return new tokens

42. Added refresh endpoint to `AuthController`:

43. Created database migration `0007_CreateRefreshTokensTable.sql`:

44. Registered `IRefreshTokenRepository` in DI container (`Program.cs`):

45. Migration execution:
    - Started API → DbUp applied migration `0007_CreateRefreshTokensTable.sql` ✅

46. Tested refresh token flow:
    - Login: Received access token + refresh token ✅
    - Refresh: Used refresh token → Got new access token + new refresh token ✅
    - Token rotation: Old refresh token marked as revoked in database, `ReplacedByToken` set ✅
    - Security: Old refresh token rejected on reuse attempt ✅
