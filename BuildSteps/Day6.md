# Day 6: Advanced Features & Audit Logging

---

## Audit Logging - Step 1: AuditLog Entity

1. Created `AuditLog` in `Domain/Entities

## Audit Logging - Step 2: AuditLogRepository Implementation

2. Created `IAuditLogRepository` interface in `Application/Common/Interfaces`
3. Created `AuditLogRepository` in `Infrastructure/Repositories`

## Audit Logging - Step 3: AuditLoggingBehavior Pipeline

4. Created `AuditLoggingBehavior` in `Application/Common/Behaviors`:
5. Build verification: `dotnet build` - successful (6.4s) ✅

## Audit Logging - Step 4: Database Migration

6. Created migration script `0007_CreateAuditLogsTable.sql` in `Infrastructure/Data/Scripts/`

## Audit Logging - Step 5: DI Registration

7. Registered `AuditLogRepository` in `Program.cs`:
8. Registered `AuditLoggingBehavior` in MediatR pipeline in `Program.cs`:

## Audit Logging - Step 6: Testing

9. Verified migration 0007 executed successfully
10. Verified AuditLogs table created with correct
11. Tested audit logging functionality
12. Verified audit data in database
13. All Create/Update/Cancel/Confirm/Delete operations are being tracked

## AvailabilityRules - Step 1: Entity & Planning

14. Created `AvailabilityRule` in `Domain/Entities

## AvailabilityRules - Step 2: DTOs (Data Transfer Objects)

15. Created `AvailabilityRuleDto` in `Application/Features/AvailabilityRules/DTOs/AvailabilityRuleDto.cs`
16. Created `CreateAvailabilityRuleRequest` (7 properties: ResourceId, DayOfWeek, StartTime, EndTime, IsActive, EffectiveFrom?, EffectiveTo?)
17. Created `CreateAvailabilityRuleResponse`
18. Created `UpdateAvailabilityRuleRequest` (5 properties: StartTime, EndTime, IsActive, EffectiveFrom?, EffectiveTo? - Note: Cannot change ResourceId or DayOfWeek)
19. Created `UpdateAvailabilityRuleResponse`
20. Created `DeleteAvailabilityRuleResponse`
21. Build verification: `dotnet build` - successful (9.8s) ✅

## AvailabilityRules - Step 3: CQRS Commands

22. Created `CreateAvailabilityRuleCommand` in `Commands/CreateAvailabilityRule`
23. Created `CreateAvailabilityRuleCommandHandler`
24. Created `CreateAvailabilityRuleCommandValidator`
25. Created `UpdateAvailabilityRuleCommand` in `Commands/UpdateAvailabilityRule`
26. Created `UpdateAvailabilityRuleCommandHandler`
27. Created `UpdateAvailabilityRuleCommandValidator`
28. Created `DeleteAvailabilityRuleCommand` in `Commands/DeleteAvailabilityRule`
29. Created `DeleteAvailabilityRuleCommandHandler`
30. Created `DeleteAvailabilityRuleCommandValidator`
31. Created `IAvailabilityRuleRepository` interface in `Application/Common/Interfaces`
32. Created `DeleteAvailabilityRuleResponse`

## AvailabilityRules - Step 4: CQRS Queries

33. Created `GetAvailabilityRuleByIdQuery` in `Queries/GetAvailabilityRuleById`
34. Created `GetAvailabilityRuleByIdQueryHandler`
35. Created `GetAvailabilityRuleByIdQueryValidator`
36. Created `GetAllAvailabilityRulesQuery` in `Queries/GetAllAvailabilityRules`
37. Created `GetAllAvailabilityRulesQueryHandler`
38. Created `GetAllAvailabilityRulesQueryValidator`
39. Updated `IAvailabilityRuleRepository` interface - added GetPagedAsync method

## AvailabilityRules - Step 5: Repository Implementation

43. Created `AvailabilityRuleRepository.cs` in `Infrastructure/Repositories`

## AvailabilityRules - Step 6: Controller

49. Created `AvailabilityRulesController` in `API/Controllers/v1`

## AvailabilityRules - Step 7: Database Migration

58. Created `0008_CreateAvailabilityRulesTable.sql` in `Infrastructure/Data/Scripts`

## AvailabilityRules - Step 8: DI Registration

66. Registered `AvailabilityRuleRepository` in `Program.cs`

## AvailabilityRules - Step 9: Testing

69. Verified migration 0008 executed: "Executing Database Server script '0008_CreateAvailabilityRulesTable.sql'" ✅
70. Verified AvailabilityRules
71. Tested data insertion
72. Verified check constraint
73. Verified check constraint
74. Verified queries
75. All components integrated successfully

## Other features - Step 1: Create Admin Endpoints to View Audit Logs

234: Created GetPaginatedAuditLogsQuery
235: Created GetPaginatedAuditLogsQueryHandler
236: Created GetPaginatedAuditLogsQueryValidator
237: Updated IAuditLogRepository Interface
238: Updated AuditLogRepository Implementation
240: Create AuditLogsController
241: Build and Verify

## Other features - Step 2: Implement Soft Delete for Entities

84. Updated domain entities with soft delete properties:
    - **Resource.cs**: Added `IsDeleted` (default false) and `DeletedAt` properties
    - **Booking.cs**: Added `IsDeleted` and `DeletedAt` properties
    - **AvailabilityRule.cs**: Added `IsDeleted` and `DeletedAt` properties

85. Created migration `0009_AddSoftDeleteSupport.sql` in `Infrastructure/Data/Scripts`:
86. Updated `IRepository<T>` interface in `Application/Common/Interfaces`
87. Updated `BaseRepository` in `Infrastructure/Repositories/BaseRepository`
88. Updated `BookingRepository`
89. Updated `AvailabilityRuleRepository`
90. Updated Delete command handlers to use SoftDeleteAsync
91. Updated `TenantRepository`
92. Migration execution:
    - Started API: `cd src/BookingSystem.API; dotnet run`

## Other features - Step 3: Add Filtering and Sorting to Booking Queries

94. Updated `GetAllBookingsQuery` in `Application/Features/Bookings/Queries/GetAllBookings`:

95. Updated `IBookingRepository` interface in `Application/Common/Interfaces`:
96. Implemented `GetPagedAsync` in `BookingRepository.cs` in `Infrastructure/Repositories`:
97. Updated `GetAllBookingsQueryHandler`:
98. Created `GetAllBookingsQueryValidator.cs`:

## Other features - Step 4: Create Statistics Endpoints

100. Created `BookingStatisticsDto` in `Application/Features/Bookings/DTOs/`:
101. Created `GetBookingStatisticsQuery` in `Application/Features/Bookings/Queries/GetBookingStatistics`:
102. Created `GetBookingStatisticsQueryValidator`:
103. Updated `IBookingRepository` interface
104. Created `GetBookingStatisticsQueryHandler`
105. Implemented `GetStatisticsAsync` in `BookingRepository.cs`:
106. Updated `BookingsController`

## Other features - Step 5: Add Rate Limiting Middleware

107. Installed AspNetCoreRateLimit package in API project:
     - `cd BookingSystem.API; dotnet add package AspNetCoreRateLimit`
108. Configured rate limiting in `appsettings.json`:
109. Updated `Program.cs` imports:
     - Added `using AspNetCoreRateLimit;`
110. Registered rate limiting services in `Program.cs`:
111. Added rate limiting middleware to pipeline:

## Other features - Step 6: Implement RefreshToken Mechanism

112. Created `RefreshToken` entity in `Domain/Entities`:
113. Updated `AuthResult` DTO to include `RefreshToken`
114. Created RefreshToken DTOs in `Application/Features/Authentication/DTOs`:
115. Created `RefreshAccessTokenCommand` in `Application/Features/Authentication/Commands/RefreshToken`:
116. Created `IRefreshTokenRepository` in `Application/Common/Interfaces`:
117. Updated `IJwtTokenService` interface:
118. Updated `JwtTokenService` implementation
119. Implemented `RefreshTokenRepository` in `Infrastructure/Repositories`:
120. Updated `LoginCommandHandler` to generate and store refresh token
121. Added refresh endpoint to `AuthController`
122. Created database migration `0010_CreateRefreshTokensTable.sql`
123. Registered `IRefreshTokenRepository` in DI container (`Program.cs`):

124. Migration execution: Started API → DbUp applied migration `0010_CreateRefreshTokensTable.sql` ✅

125. Tested refresh token flow:
     - Login: Received access token + refresh token ✅
     - Refresh: Used refresh token → Got new access token + new refresh token ✅
     - Token rotation: Old refresh token marked as revoked in database, ReplacedByToken set ✅
     - Security: Old refresh token rejected on reuse attempt ✅
