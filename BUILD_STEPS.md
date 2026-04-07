# Build Steps Record

**Purpose**: Sequential numbered list of every implementation step taken during development.

**Instructions**: Add each step as we work - exact commands, code changes, and key decisions.

---

## Day 1:

### Project Setup & Architecture

1. Created src folder and navigated into it: `mkdir src; cd src`
2. Created .NET solution file: `dotnet new sln -n BookingSystem`
3. Created Domain project (class library): `dotnet new classlib -n BookingSystem.Domain`
4. Created Application project (class library): `dotnet new classlib -n BookingSystem.Application`
5. Created Infrastructure project (class library): `dotnet new classlib -n BookingSystem.Infrastructure`
6. Created API project (ASP.NET Core Web API): `dotnet new webapi -n BookingSystem.API`
7. Added all projects to solution:
   - `dotnet sln add BookingSystem.Domain/BookingSystem.Domain.csproj`
   - `dotnet sln add BookingSystem.Application/BookingSystem.Application.csproj`
   - `dotnet sln add BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj`
   - `dotnet sln add BookingSystem.API/BookingSystem.API.csproj`
8. Configured project references (Clean Architecture dependency flow):
   - `dotnet add BookingSystem.API/BookingSystem.API.csproj reference BookingSystem.Application/BookingSystem.Application.csproj`
   - `dotnet add BookingSystem.API/BookingSystem.API.csproj reference BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj`
   - `dotnet add BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj reference BookingSystem.Application/BookingSystem.Application.csproj`
   - `dotnet add BookingSystem.Application/BookingSystem.Application.csproj reference BookingSystem.Domain/BookingSystem.Domain.csproj`

### Docker & PostgreSQL Setup

9. Created `docker-compose.yml` with PostgreSQL 16 service configuration
   - Image: postgres:16-alpine
   - Database: BookingSystemDB
   - Port: 5432
   - Credentials: postgres/postgres (development only)
10. Started PostgreSQL container: `docker-compose up -d`

### NuGet Packages Installation

11. Installed Dapper and Npgsql in Infrastructure project:
    - `cd src/BookingSystem.Infrastructure`
    - `dotnet add package Dapper`
    - `dotnet add package Npgsql`
12. Installed MediatR in Application project:
    - `cd ../BookingSystem.Application`
    - `dotnet add package MediatR`
13. Installed Serilog in API project:
    - `cd ../BookingSystem.API`
    - `dotnet add package Serilog.AspNetCore`
    - `dotnet add package Serilog.Sinks.Console`
    - `dotnet add package Serilog.Sinks.File`
14. Installed FluentValidation in Application project:
    - `cd ../BookingSystem.Application`
    - `dotnet add package FluentValidation.DependencyInjectionExtensions`
15. Installed AutoMapper in Application project:
    - `dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection`

### Configuration

16. Configured Serilog in `Program.cs`
17. Configured MediatR in `Program.cs`, Created `AssemblyReference.cs` in `BookingSystem.Application`
18. Added PostgreSQL connection string to `appsettings.Development.json`
19. Configured FluentValidation in `Program.cs`
20. Configured AutoMapper in `Program.cs`

### ⭐ VS Code C# Auto-Formatting Setup

**Enable auto-format on save for better code quality:**

Press `Ctrl+Shift+P` → Type "Preferences: Open User Settings (JSON)"

Add these settings:

```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.organizeImports": "explicit"
  },
  "[csharp]": {
    "editor.formatOnSave": true,
    "editor.defaultFormatter": "ms-dotnettools.csharp"
  }
}
```

**Required Extension:** C# Dev Kit by Microsoft (`ms-dotnettools.csharp`)

**Benefits:**

- Auto-formats code on every save
- Organizes imports (removes unused, sorts alphabetically)
- Consistent code style across the project
- Works with EditorConfig for team-wide consistency

---

## Day 2: Core Infrastructure & Multi-tenancy

### Middleware

21. Created `GlobalExceptionHandlerMiddleware.cs` in API/Middleware
22. Registered middleware in `Program.cs`

### API Versioning

23. Installed API versioning packages:
    - `dotnet add package Asp.Versioning.Http`
    - `dotnet add package Asp.Versioning.Mvc.ApiExplorer`
24. Configured API versioning in `Program.cs` (default v1.0)
25. Added Controllers support with `AddControllers()` and `MapControllers()`

### Swagger/OpenAPI

26. Installed Swashbuckle: `dotnet add package Swashbuckle.AspNetCore`
27. Configured Swagger in `Program.cs` with API documentation

### Common Patterns

28. Created `PagedResult<T>` class in Application/Common/Models for pagination

### Database Migrations

29. Installed DbUp: `dotnet add package dbup-postgresql` in Infrastructure project
30. Created `DatabaseMigration.cs` class with migration runner
31. Created initial schema SQL script: `0001_InitialSchema.sql` (Tenants, Users, Roles, UserRoles)
32. Configured .csproj to embed SQL scripts as resources
33. Integrated migration runner in `Program.cs` to run on startup

### Repository Pattern

34. Created `IDbConnectionFactory` interface in Application layer
35. Implemented `DbConnectionFactory` in Infrastructure using Npgsql
36. Registered factory in DI container as singleton
37. Fixed package conflict: Removed `Microsoft.AspNetCore.OpenApi` (conflicted with Swashbuckle 10.1.7)
38. Verified application startup: `dotnet run --project ...\bulletproof-dotnet-react\src\BookingSystem.API\BookingSystem.API.csproj`
39. Database migrations executed successfully, all tables created
40. Verified database schema with `docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "\dt"`
41. Verified seed data: 3 roles (TenantAdmin, Manager, User) inserted successfully
42. Updated INSTALLATION_GUIDE.md to reflect working package configuration

### Multi-Tenancy (TenantContext Service)

43. Created `ITenantContext` interface in Application/Common/Interfaces
44. Implemented `TenantContext` service in Infrastructure/Services with SetTenantId() and Clear() methods
45. Created `TenantResolutionMiddleware` in API/Middleware to extract tenant from X-Tenant-Id header
46. Registered TenantContext as scoped service in DI container
47. Added TenantResolutionMiddleware to pipeline (after global error handling, before controllers)
48. Middleware skips Swagger and health check endpoints
49. Middleware validates X-Tenant-Id header format (GUID) and returns 400 if invalid or missing
50. Created test controller `TenantController` (v1) to verify tenant resolution
51. Tested middleware:
    - ✅ Request without header: 400 "X-Tenant-Id header is required"
    - ✅ Request with invalid GUID: 400 "Invalid X-Tenant-Id header format"
    - ✅ Request with valid GUID: 200 with tenantId and isResolved=true
    - ✅ Swagger endpoint bypasses tenant check

### Multi-Tenant Query Filter (Repository Pattern with Dapper)

52. Installed Dapper in Application project for extension methods
53. Created `DapperExtensions.cs` with tenant-aware query methods:
    - `QueryWithTenantAsync<T>()` - SELECT with automatic TenantId filtering
    - `QuerySingleOrDefaultWithTenantAsync<T>()` - Single result with tenant filter
    - `ExecuteWithTenantAsync()` - INSERT/UPDATE/DELETE with tenant filter
54. Created `IRepository<T>` base interface in Application/Common/Interfaces
55. Created `BaseRepository<T>` abstract class in Infrastructure/Repositories with:
    - Automatic TenantId injection in all queries
    - GetByIdAsync, GetAllAsync, DeleteAsync, ExistsAsync with tenant filtering
    - Abstract AddAsync and UpdateAsync for entity-specific implementation
56. Created `User` entity in Domain/Entities with UUID Id and TenantId
57. Created `IUserRepository` interface extending IRepository<User>
58. Implemented `UserRepository` with tenant-aware CRUD operations
59. Registered `IUserRepository` in DI container as scoped service
60. Created migration `0002_ConvertToUUID.sql` to convert all ID columns from SERIAL to UUID
61. Migration drops and recreates tables with `gen_random_uuid()` for PostgreSQL UUID support
62. Created `UsersController` (v1) to demonstrate multi-tenant filtering
63. Created test tenants in database (aaaaa... and bbbbb...)
64. Tested multi-tenant data isolation:
    - ✅ Created user for Tenant 1 (john@tenant1.com)
    - ✅ Created user for Tenant 2 (jane@tenant2.com)
    - ✅ Tenant 1 query returns only Tenant 1 users
    - ✅ Tenant 2 query returns only Tenant 2 users
    - ✅ Cross-tenant access blocked (Tenant 2 cannot access Tenant 1's user - returns 404)
    - ✅ Same email allowed in different tenants (john@tenant1.com exists in both tenants)
    - ✅ Multi-tenant data isolation fully functional

---

## Day 3: Authentication & Authorization

### Password Hashing Setup

65. Installed BCrypt.Net-Next package in Infrastructure project:
    - `cd src/BookingSystem.Infrastructure`
    - `dotnet add package BCrypt.Net-Next --version 4.0.3`
    - Package added to `BookingSystem.Infrastructure.csproj`
66. Created `IPasswordHasher` interface in `Application/Common/Interfaces/IPasswordHasher.cs`:
67. Implemented `PasswordHasher` service in `Infrastructure/Services/PasswordHasher.cs`:
    - Uses BCrypt.Net.BCrypt for hashing (adaptive algorithm with built-in salt)
68. Registered `IPasswordHasher` service in DI container (`Program.cs`):
    - `builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();`
    - Scoped lifetime: new instance per HTTP request

### JWT Token Generation

69. Installed JWT packages:
    - API project: `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.1`
    - Infrastructure project: `dotnet add package System.IdentityModel.Tokens.Jwt` (version 8.17.0 installed)
70. Added JWT configuration to `appsettings.json`:
71. Added JWT configuration to `appsettings.Development.json`
72. Created `IJwtTokenService` interface in `Application/Common/Interfaces/IJwtTokenService.cs`:`
73. Implemented `JwtTokenService` in `Infrastructure/Services/JwtTokenService.cs`:
    - Uses `System.IdentityModel.Tokens.Jwt` for token generation
    - Creates claims: NameIdentifier (userId), Email, Jti (unique token ID), and Role claims
    - Signs token with HMAC SHA256 using symmetric key from configuration
    - Token expiration based on configuration (default 60 minutes)
74. Registered `IJwtTokenService` in DI container (`Program.cs`)
75. Build verification: `dotnet build` - successful (6.0s)

### Tenant & User Entities for Authentication

76. Created `Tenant` entity in `Domain/Entities/Tenant.cs`
77. Verified `User` entity already has authentication fields
78. Created `ITenantRepository` interface in `Application/Common/Interfaces/ITenantRepository.cs`
79. Implemented `TenantRepository` in `Infrastructure/Repositories/TenantRepository.cs`:
80. Registered `ITenantRepository` in DI container (`Program.cs`)

### Authentication DTOs (Data Transfer Objects)

81. Created `AuthResult` DTO in `Application/Features/Authentication/DTOs/AuthResult.cs`:
82. Created `RegisterTenantRequest` DTO
83. Created `RegisterTenantResponse` DTO
84. Created `RegisterUserRequest` DTO
85. Created `RegisterUserResponse` DTO
86. Created `LoginRequest` DTO
87. Created `LoginResponse` DTO

### RegisterTenant Command (CQRS)

88. Created `RegisterTenantCommand` in `Application/Features/Authentication/Commands/RegisterTenant/`
89. Created `RegisterTenantCommandHandler`
90. Created `RegisterTenantCommandValidator` (FluentValidation)

### RegisterUser Command (CQRS)

91. Created `RegisterUserCommand` in `Application/Features/Authentication/Commands/RegisterUser/`
92. Created `RegisterUserCommandHandler`
93. Created `RegisterUserCommandValidator` (FluentValidation)

### Login Command (CQRS)

94. Created `LoginCommand` in `Application/Features/Authentication/Commands/Login/`
95. Created `LoginCommandHandler`
96. Created `LoginCommandValidator` (FluentValidation)

### Authentication Controller (API Endpoints)

97. Created `AuthController` in `API/Controllers/v1/AuthController.cs`

### JWT Authentication Middleware

98. Configured JWT authentication in `Program.cs`
99. Added authentication middleware to pipeline:
    - `app.UseAuthentication()` before `app.UseAuthorization()`

### Authorization Policies & Protected Endpoints

100. Configured authorization policies in `Program.cs`
101. Added `[Authorize]` attribute to protected controllers
102. Added `[AllowAnonymous]` attribute to `AuthController`:


    - Allows unauthenticated access to register-tenant, register-user, login endpoints
    - Required because FallbackPolicy would otherwise block these endpoints

---

## Day 4: Resources CRUD

### Resource Entity (Domain Layer)

103. Created `Resource` entity in `Domain/Entities/Resource.cs`

### Resource DTOs (Application Layer)

104. Created `ResourceDto` in `Application/Features/Resources/DTOs/ResourceDto.cs`
105. Created `CreateResourceRequest` DTO
106. Created `CreateResourceResponse` DTO
107. Created `UpdateResourceRequest` DTO
108. Created `UpdateResourceResponse` DTO
109. Created `DeleteResourceResponse` DTO

### Resource Commands (CQRS)

110. Created `IResourceRepository` interface in `Application/Common/Interfaces/IResourceRepository.cs`
111. Created `CreateResourceCommand` in `Application/Features/Resources/Commands/CreateResource/`
112. Created `CreateResourceCommandHandler`
113. Created `CreateResourceCommandValidator`
114. Created `UpdateResourceCommand` in `Application/Features/Resources/Commands/UpdateResource/`
115. Created `UpdateResourceCommandHandler`
116. Created `UpdateResourceCommandValidator`
117. Created `DeleteResourceCommand` in `Application/Features/Resources/Commands/DeleteResource/`
118. Created `DeleteResourceCommandHandler`
119. Created `DeleteResourceCommandValidator`

### Resource Queries (CQRS)

120. Created `GetResourceByIdQuery` in `Application/Features/Resources/Queries/GetResourceById/`
121. Created `GetResourceByIdQueryHandler`
122. Created `GetResourceByIdQueryValidator`
123. Created `GetAllResourcesQuery` in `Application/Features/Resources/Queries/GetAllResources/`
124. Created `GetAllResourcesQueryHandler`
125. Created `GetAllResourcesQueryValidator`
126. Added `GetPagedAsync` method to `IRepository<T>` interface
127. Implemented `GetPagedAsync` in `BaseRepository<T>`
128. Implemented `GetPagedAsync` in `TenantRepository`

### Resource Repository Implementation

129. Created `ResourceRepository` in `Infrastructure/Repositories/ResourceRepository.cs`
130. Registered `IResourceRepository` in DI container (`Program.cs`):


    - `builder.Services.AddScoped<IResourceRepository, ResourceRepository>();`

### Resources Controller (API Endpoints)

131. Created `ResourcesController` in `API/Controllers/v1/ResourcesController.cs`

### Database Migration for Resources Table

132. Created migration script `0003_CreateResourcesTable.sql`

133. Executed migration: Rebuild and restart API → DbUp applied 0003_CreateResourcesTable.sql
134. Verified table creation: `docker exec bookingsystem-db psql -U postgres -d BookingSystemDB -c "\d resources"`

135. **Register Tenant (Acme Corp)**:
     `Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/register-tenant" -Method POST -Body '{"tenantName":"Acme Corp","email":"admin@acme.com","password":"Admin1234","firstName":"Alice","lastName":"Admin","plan":"Pro"}' -ContentType "application/json"`
     Result: Got JWT token + tenantId `4b47f363-8f8d-4dce-bcec-4ee66d2a2eb4`
136. **Create Resource**:
     `POST /api/v1/resources` with headers (Authorization: Bearer {token}, X-Tenant-Id: {guid})
     Result: Conference Room A created, ID `3ebb51d2-36af-4a56-89e5-17db7eec34a5`
137. **List Resources (Paginated)**:
     `GET /api/v1/resources?pageNumber=1&pageSize=10`
     Result: PagedResult with 1 item, totalCount=1, totalPages=1
138. **Get Resource by ID**:
     `GET /api/v1/resources/3ebb51d2-36af-4a56-89e5-17db7eec34a5`
     Result: Single ResourceDto returned
139. **Update Resource**:
     `PUT /api/v1/resources/3ebb51d2-36af-4a56-89e5-17db7eec34a5`
     Result: Name changed to "Conference Room A (Updated)", capacity 10→12, updatedAt set
140. **Delete Resource**:
     `DELETE /api/v1/resources/3ebb51d2-36af-4a56-89e5-17db7eec34a5`
     Result: 200 OK "Resource deleted successfully"
141. **Verify Deletion**:
     `GET /api/v1/resources`
     Result: Empty list, totalCount=0

### Multi-Tenant Isolation Testing

142. Created resource for Acme Corp: "Acme Meeting Room 1" (ID: `47fd25cb-ad01-4e64-ba9a-4a4e28582b1c`)
143. Registered second tenant: TechCo (`email:admin@techco.com`, tenantId: `fa6b63f7-55ae-4660-b1de-13a7d0258902`)
144. Created resource for TechCo: "TechCo Lab Room" (Laboratory)
145. Registered third tenant: GlobalCorp (`email:admin@globalcorp.com`, tenantId: `e13ea0c3-b658-424b-91da-d286df05703e`)
146. Created resource for GlobalCorp: "GlobalCorp Boardroom" (ID: `b1846791-47f2-4a27-860f-40977d1feb18`)
147. **Verified database isolation**: `docker exec bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT id, name, resourcetype, tenantid FROM resources"`
     Result: 3 resources, each with different tenantId

---

## Day 5: Bookings CRUD & Business Logic

### Booking Entity & Enum (Domain Layer)

148. In `Domain/Entities/Booking.cs` creted:
     - `BookingStatus`
     - `Booking`

### Booking DTOs (Application Layer)

149. In `Application/Features/Bookings/DTOs/BookingDto.cs` created:
     - `BookingDto`
     - `CreateBookingRequest`
     - `CreateBookingResponse`
     - `UpdateBookingRequest`
     - `UpdateBookingResponse`
     - `CancelBookingResponse`
     - `ConfirmBookingResponse`
     - `DeleteBookingResponse`

### Booking Commands (Application Layer)

150. In `Application/Features/Bookings/Commands/` created:
     - `CreateBooking`
     - `UpdateBooking`
     - `CancelBooking`
     - `ConfirmBooking`
     - `DeleteBooking`

### Booking Queries (Application Layer)

151. In `Application/Features/Bookings/Queries/` created:
     - `GetBookingById`
     - `GetAllBookings`

### Booking Repository Interface (Application Layer)

152. In `Application/Common/Interfaces/IBookingRepository.cs`:
     - created `IBookingRepository`
     - updated `GetAllBookingsQueryHandler`

### Booking Repository Implementation (Infrastructure Layer)

153. In `Infrastructure/Repositories/BookingRepository.cs` created`BookingRepository`

### Bookings Controller (API Layer)

154. In `API/Controllers/v1` created `BookingsController`

### Database Migration (Infrastructure Layer)

155. In `Infrastructure/Data/Scripts/` created `0006_CreateBookingsTable.sql`

### Dependency Injection Registration (API Layer)

156. In `API/Program.cs` registered `IBookingRepository` → `BookingRepository`

### Bug Fixes & Missing Services

157. Testing revealed missing services and bugs:
     - Created `UserLoginDto` in `LoginCommandHandler.cs`
     - Created `ICurrentUserService` interface in `Application/Common/Interfaces/`
     - Created `CurrentUserService` in `Infrastructure/Services/` (extracts userId from JWT NameIdentifier claim)
     - Added `Microsoft.AspNetCore.Http.Abstractions` package to Infrastructure project
     - Registered `HttpContextAccessor` and `ICurrentUserService` in `Program.cs`
     - Updated `CreateBookingCommandHandler` and `UpdateBookingCommandHandler` to use `ICurrentUserService.UserId`
     - Created `BookingMappingProfile.cs` in `Application/Common/Mappings/` for Booking → BookingDto mapping
     - Added `ManagerOrAbove` policy to `Program.cs` authorization configuration

### Testing Day 5 - Bookings CRUD & Business Logic

158. Tested all booking endpoints successfully:
     - **Create**: POST `/api/v1/bookings` → Created booking with userId from JWT, Status=Pending
     - **List**: GET `/api/v1/bookings?pageNumber=1&pageSize=10` → Returned paginated results with AutoMapper DTOs
     - **Update**: PUT `/api/v1/bookings/{id}` → Updated title, description, notes for Pending booking
     - **Confirm**: POST `/api/v1/bookings/{id}/confirm` (admin) → Changed status Pending → Confirmed ✅
     - **Cancel**: POST `/api/v1/bookings/{id}/cancel` → Changed status to Cancelled ✅
     - **Delete**: DELETE `/api/v1/bookings/{id}` (admin) → Hard delete from database ✅
     - **Multi-tenant isolation**: All queries tenant-filtered via BaseRepository
     - **HasConflictAsync SQL**: Verified time overlap detection `(StartTime < EndTime) AND (EndTime > StartTime)` working correctly
     - All authorization policies enforced (ManagerOrAbove for confirm, AdminOnly for delete) ✅

---

## Day 6: Advanced Features & Audit Logging

### Audit Logging - Step 1: AuditLog Entity

159. Created `AuditLog` in `Domain/Entities

### Audit Logging - Step 2: AuditLogRepository Implementation

160. Created `IAuditLogRepository` interface in `Application/Common/Interfaces`
161. Created `AuditLogRepository` in `Infrastructure/Repositories`

### Audit Logging - Step 3: AuditLoggingBehavior Pipeline

162. Created `AuditLoggingBehavior` in `Application/Common/Behaviors`:
163. Build verification: `dotnet build` - successful (6.4s) ✅

### Audit Logging - Step 4: Database Migration

164. Created migration script `0007_CreateAuditLogsTable.sql` in `Infrastructure/Data/Scripts/`

### Audit Logging - Step 5: DI Registration

165. Registered `AuditLogRepository` in `Program.cs`:
166. Registered `AuditLoggingBehavior` in MediatR pipeline in `Program.cs`:

### Audit Logging - Step 6: Testing

167. Verified migration 0007 executed successfully
168. Verified AuditLogs table created with correct
169. Tested audit logging functionality
170. Verified audit data in database
171. All Create/Update/Cancel/Confirm/Delete operations are being tracked

### AvailabilityRules - Step 1: Entity & Planning

172. Created `AvailabilityRule` in `Domain/Entities

### AvailabilityRules - Step 2: DTOs (Data Transfer Objects)

173. Created `AvailabilityRuleDto` in `Application/Features/AvailabilityRules/DTOs/AvailabilityRuleDto.cs`
174. Created `CreateAvailabilityRuleRequest` (7 properties: ResourceId, DayOfWeek, StartTime, EndTime, IsActive, EffectiveFrom?, EffectiveTo?)
175. Created `CreateAvailabilityRuleResponse`
176. Created `UpdateAvailabilityRuleRequest` (5 properties: StartTime, EndTime, IsActive, EffectiveFrom?, EffectiveTo? - Note: Cannot change ResourceId or DayOfWeek)
177. Created `UpdateAvailabilityRuleResponse`
178. Created `DeleteAvailabilityRuleResponse`
179. Build verification: `dotnet build` - successful (9.8s) ✅

### AvailabilityRules - Step 3: CQRS Commands

180. Created `CreateAvailabilityRuleCommand` in `Commands/CreateAvailabilityRule`
181. Created `CreateAvailabilityRuleCommandHandler`
182. Created `CreateAvailabilityRuleCommandValidator`
183. Created `UpdateAvailabilityRuleCommand` in `Commands/UpdateAvailabilityRule`
184. Created `UpdateAvailabilityRuleCommandHandler`
185. Created `UpdateAvailabilityRuleCommandValidator`
186. Created `DeleteAvailabilityRuleCommand` in `Commands/DeleteAvailabilityRule`
187. Created `DeleteAvailabilityRuleCommandHandler`
188. Created `DeleteAvailabilityRuleCommandValidator`
189. Created `IAvailabilityRuleRepository` interface in `Application/Common/Interfaces`
190. Created `DeleteAvailabilityRuleResponse`

### AvailabilityRules - Step 4: CQRS Queries

191. Created `GetAvailabilityRuleByIdQuery` in `Queries/GetAvailabilityRuleById`
192. Created `GetAvailabilityRuleByIdQueryHandler`
193. Created `GetAvailabilityRuleByIdQueryValidator`
194. Created `GetAllAvailabilityRulesQuery` in `Queries/GetAllAvailabilityRules`
195. Created `GetAllAvailabilityRulesQueryHandler`
196. Created `GetAllAvailabilityRulesQueryValidator`
197. Updated `IAvailabilityRuleRepository`

### AvailabilityRules - Step 4 (continued):

197. Updated `IAvailabilityRuleRepository` interface - added GetPagedAsync method

### AvailabilityRules - Step 5: Repository Implementation

201. Created `AvailabilityRuleRepository.cs` in `Infrastructure/Repositories`

### AvailabilityRules - Step 6: Controller

207. Created `AvailabilityRulesController` in `API/Controllers/v1`

### AvailabilityRules - Step 7: Database Migration

216. Created `0008_CreateAvailabilityRulesTable.sql` in `Infrastructure/Data/Scripts`

### AvailabilityRules - Step 8: DI Registration

224. Registered `AvailabilityRuleRepository` in `Program.cs`

### AvailabilityRules - Step 9: Testing

227. Verified migration 0008 executed: "Executing Database Server script '0008_CreateAvailabilityRulesTable.sql'" ✅
228. Verified AvailabilityRules
229. Tested data insertion
230. Verified check constraint
231. Verified check constraint
232. Verified queries
233. All components integrated successfully

### Other features - Step 1: Create Admin Endpoints to View Audit Logs

234: Created GetPaginatedAuditLogsQuery
235: Created GetPaginatedAuditLogsQueryHandler
236: Created GetPaginatedAuditLogsQueryValidator
237: Updated IAuditLogRepository Interface
238: Updated AuditLogRepository Implementation
240: Create AuditLogsController
241: Build and Verify

### Other features - Step 2: Implement Soft Delete for Entities

242. Updated domain entities with soft delete properties:
     - **Resource.cs**: Added `IsDeleted` (default false) and `DeletedAt` properties
     - **Booking.cs**: Added `IsDeleted` and `DeletedAt` properties
     - **AvailabilityRule.cs**: Added `IsDeleted` and `DeletedAt` properties

243. Created migration `0009_AddSoftDeleteSupport.sql` in `Infrastructure/Data/Scripts`:
244. Updated `IRepository<T>` interface in `Application/Common/Interfaces`
245. Updated `BaseRepository` in `Infrastructure/Repositories/BaseRepository`
246. Updated `BookingRepository`
247. Updated `AvailabilityRuleRepository`
248. Updated Delete command handlers to use SoftDeleteAsync
249. Updated `TenantRepository`
250. Migration execution:
     - Started API: `cd src/BookingSystem.API; dotnet run`

### Other features - Step 3: Add Filtering and Sorting to Booking Queries

252. Updated `GetAllBookingsQuery` in `Application/Features/Bookings/Queries/GetAllBookings`:

253. Updated `IBookingRepository` interface in `Application/Common/Interfaces`:
254. Implemented `GetPagedAsync` in `BookingRepository.cs` in `Infrastructure/Repositories`:
255. Updated `GetAllBookingsQueryHandler`:
256. Created `GetAllBookingsQueryValidator.cs`:

### Other features - Step 4: Create Statistics Endpoints

258. Created `BookingStatisticsDto` in `Application/Features/Bookings/DTOs/`:
259. Created `GetBookingStatisticsQuery` in `Application/Features/Bookings/Queries/GetBookingStatistics`:
260. Created `GetBookingStatisticsQueryValidator`:
261. Updated `IBookingRepository` interface
262. Created `GetBookingStatisticsQueryHandler`
263. Implemented `GetStatisticsAsync` in `BookingRepository.cs`:
264. Updated `BookingsController`

### Other features - Step 5: Add Rate Limiting Middleware

265. Installed AspNetCoreRateLimit package in API project:
     - `cd BookingSystem.API; dotnet add package AspNetCoreRateLimit`
266. Configured rate limiting in `appsettings.json`:
267. Updated `Program.cs` imports:
     - Added `using AspNetCoreRateLimit;`
268. Registered rate limiting services in `Program.cs`:
269. Added rate limiting middleware to pipeline:

### Other features - Step 6: Implement RefreshToken Mechanism

270. Created `RefreshToken` entity in `Domain/Entities`:
271. Updated `AuthResult` DTO to include `RefreshToken`
272. Created RefreshToken DTOs in `Application/Features/Authentication/DTOs`:
273. Created `RefreshAccessTokenCommand` in `Application/Features/Authentication/Commands/RefreshToken`:
274. Created `IRefreshTokenRepository` in `Application/Common/Interfaces`:
275. Updated `IJwtTokenService` interface:
276. Updated `JwtTokenService` implementation
277. Implemented `RefreshTokenRepository` in `Infrastructure/Repositories`:
278. Updated `LoginCommandHandler` to generate and store refresh token
279. Added refresh endpoint to `AuthController`
280. Created database migration `0010_CreateRefreshTokensTable.sql`
281. Registered `IRefreshTokenRepository` in DI container (`Program.cs`):

282. Migration execution: Started API → DbUp applied migration `0010_CreateRefreshTokensTable.sql` ✅

283. Tested refresh token flow:
     - Login: Received access token + refresh token ✅
     - Refresh: Used refresh token → Got new access token + new refresh token ✅
     - Token rotation: Old refresh token marked as revoked in database, ReplacedByToken set ✅
     - Security: Old refresh token rejected on reuse attempt ✅

---

## Day 7:

### Backend Testing - Step 1: Set up xUnit test projects

284. Created xUnit test project for unit tests
285. Created xUnit test project for integration tests
286. Added test projects to solution
287. Configured project references for UnitTests
288. Configured project references for IntegrationTests
289. Deleted default test files:
     - `Remove-Item BookingSystem.UnitTests/UnitTest1.cs -Force`
     - `Remove-Item BookingSystem.IntegrationTests/UnitTest1.cs -Force`
290. Created folder structure in UnitTests project:
     - `Domain/Entities`
     - `Application/Commands`
     - `Application/Queries`
     - `Application/Validators`
291. Created folder structure in IntegrationTests project:
     - `Controllers`
     - `Infrastructure`
292. Verified build: Test projects compiled successfully ✅

### Backend Testing - Step 2: Install testing NuGet packages

293. Installed testing packages for UnitTests project:
     - `dotnet add package Moq` → version 4.20.72
     - `dotnet add package FluentAssertions` → version 8.9.0
294. Installed testing packages for IntegrationTests project:
     - `dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.*` → version 9.0.14 (WebApplicationFactory for in-memory testing)
     - `dotnet add package Testcontainers.PostgreSql` → version 4.11.0 (PostgreSQL container for integration tests)
     - `dotnet add package FluentAssertions` → version 8.9.0 (readable assertions)

### Backend Testing - Step 3: Write unit tests for Domain entities

295. Created `RefreshTokenTests` with 9 test methods
296. Created `BookingTests` with 7 test methods
297. Created `AvailabilityRuleTests`
298. Executed unit tests

### Backend Testing - Step 4: Write unit tests for CQRS Command Handlers

299. Created `CreateResourceCommandHandlerTests` in `BookingSystem.UnitTests/Application/Commands`
300. Created `CreateBookingCommandHandlerTests` in `BookingSystem.UnitTests/Application/Commands`
301. Executed unit tests

### Backend Testing - Step 5: Write unit tests for FluentValidation Validators

302. Created `CreateResourceCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
303. Created `CreateBookingCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
304. Created `LoginCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
305. Test run

### Backend Testing - Step 6: Set up Integration test infrastructure (WebApplicationFactory)

306. Made Program class accessible to integration tests
307. Created `IntegrationTestWebApplicationFactory` in `BookingSystem.IntegrationTests`
308. Created `IntegrationTestBase`
309. Created `InfrastructureSmokeTests`
310. Executed integration smoke tests

### Backend Testing - Step 7: Configure Testcontainers for PostgreSQL

311. Created `DatabaseFixture` in `BookingSystem.IntegrationTests/Infrastructure`:
312. Created `TestWebApplicationFactory` in `BookingSystem.IntegrationTests/Infrastructure`
313. Updated `IntegrationTestBase`:
314. Updated `InfrastructureSmokeTests`:
315. Executed integration tests

316. Created DatabaseFixture with PostgreSqlContainer lifecycle management
317. Created TestWebApplicationFactory with database connection injection
318. Updated IntegrationTestBase to use DatabaseFixture,

### Backend Testing - Step 8: Write integration tests for Auth endpoints

317. Created AuthControllerTests in BookingSystem.IntegrationTests/Controllers
318. Updated GlobalExceptionHandlerMiddleware to map UnauthorizedAccessException → 401, ArgumentException → 400
319. Updated RegisterTenantCommandHandler to generate and store RefreshToken
320. Updated RegisterUserCommandHandler to generate and store RefreshToken
321. Fixed IntegrationTestBase helper methods to return full DTO objects (RegisterTenantResponse, LoginResponse)
322. Test execution

### Step 9: Resources CRUD Integration Tests

**Goal**: Write comprehensive integration tests for Resources endpoints with authentication, multi-tenant isolation, pagination, and database verification.

323. Created ResourcesControllerTests in BookingSystem.IntegrationTests/Controllers with 12 tests:
     - CreateResource_WithValidData_ShouldReturn201AndResource
     - GetAllResources_WithPagination_ShouldReturnPagedResults
     - GetResourceById_WithValidId_ShouldReturn200AndResource
     - GetResourceById_WithInvalidId_ShouldReturn404
     - UpdateResource_WithValidData_ShouldReturn200AndUpdatedResource
     - DeleteResource_WithValidId_ShouldReturn200AndSoftDelete (verifies is_deleted, deleted_at)
     - MultiTenant_ResourceIsolation_TenantCannotAccessOtherTenantsResources
     - GetAllResources_WithResourceTypeFilter_ShouldReturnFilteredResults
     - CreateResource_WithoutAuthentication_ShouldReturn401
     - CreateResource_WithoutTenantHeader_ShouldReturn400
     - UpdateResource_OfDifferentTenant_ShouldReturn404
     - Diagnostic_CreateResourceWithLoginToken_ShouldWork (for debugging)

324. Updated TestWebApplicationFactory to explicitly override JWT validation

### Step 10: Bookings Integration Tests

**Goal**: Write comprehensive integration tests for Bookings endpoints including conflict detection, status workflows, and authorization.

335. Created BookingsControllerTests in BookingSystem.IntegrationTests/Controllers with 11 tests:
     - CreateBooking_WithValidData_ShouldReturn201AndBooking
     - CreateBooking_WithOverlappingTimes_ShouldReturn400Conflict (validates conflict detection)
     - GetBookingById_WithValidId_ShouldReturn200AndBooking
     - GetBookingById_WithInvalidId_ShouldReturn404
     - GetAllBookings_WithPagination_ShouldReturnPagedResults
     - UpdateBooking_WithValidData_ShouldReturn200AndUpdatedBooking
     - CancelBooking_WithValidId_ShouldReturn200AndCancelledBooking
     - MultiTenant_BookingIsolation_TenantCannotAccessOtherTenantsBookings
     - CreateBooking_WithoutAuthentication_ShouldReturn401
     - CreateBooking_WithoutTenantHeader_ShouldReturn400

336. Added `InvalidOperationException`

### Step 11: Architecture Tests (NetArchTest.Rules)

**Goal**: Validate Clean Architecture principles and enforce coding conventions using automated architecture tests.

341. Installed NetArchTest.Rules package: `dotnet add package NetArchTest.Rules` (version 1.3.2)
342. Created AssemblyReference
343. Created ArchitectureTests in BookingSystem.UnitTests/Architecture with 15 comprehensive tests:

     **Layer Dependency Tests**:
     - Domain_Should_NotHaveAnyDependencies (Domain is innermost layer)
     - Application_Should_OnlyDependOnDomain (Application layer isolation)
     - Infrastructure_Should_NotDependOnAPI (Infrastructure independent of API)
     - API_Controllers_Should_NotDirectlyUseRepositories (Controllers use MediatR, not repositories)

     **Naming Convention Tests**:
     - Commands_Should_EndWithCommand
     - Queries_Should_EndWithQuery
     - Handlers_Should_EndWithHandler
     - Validators_Should_EndWithValidator
     - Controllers_Should_EndWithController
     - Repositories_Should_EndWithRepository (excludes abstract base classes)

     **CQRS Pattern Tests**:
     - Handlers_Should_ResideInCorrectNamespace (all in Features namespace)
     - DTOs_Should_ResideInDTOsFolder (Request/Response/Dto classes)
     - Entities_Should_ResideInDomainLayer (Domain.Entities namespace)

     **Interface and Implementation Tests**:
     - Repositories_Should_ImplementIRepository (concrete classes)
     - Middleware_Should_ResideInAPILayer (API.Middleware namespace)

344. Added project references to UnitTests
345. Test execution

### Step 12: Code Coverage Measurement

**Goal**: Measure code coverage from unit and integration tests, generate reports, and document coverage by layer.

348. Installed coverlet.msbuild for code coverage collection
349. Ran unit tests with coverage collection:
350. Installed ReportGenerator global tool for HTML coverage reports Version: 5.5.4
351. Generated HTML coverage report from unit tests
352. **Unit Test Coverage Statistics** (by layer):
     - **Domain Layer**: 64% line coverage, 100% branch coverage
       - Booking entity: 100% covered
       - AvailabilityRule entity: 100% covered
       - RefreshToken entity: 100% covered
       - Resource entity: 90.9% covered
       - User, Tenant, AuditLog: 0% covered (no unit tests yet)
     - **Application Layer**: 12% line coverage, 5% branch coverage
       - Commands, Validators, Handlers: Partial coverage
       - Focus areas: CreateResource, CreateBooking, Login operations
     - **Infrastructure Layer**: 0% line coverage (expected - tested via integration tests)
     - **API Layer**: 0% line coverage (expected - tested via integration tests)
     - **Overall**: 7.82% line coverage, 3.92% branch coverage (243/3104 lines covered)

353. Attempted integration test coverage measurement
354. **Coverage Analysis Summary**:
     - Unit tests provide **accurate coverage** for Domain (64%) and Application (12%) layers
     - Integration tests validate **end-to-end functionality** but don't contribute to coverage metrics reliably
     - Combined test suite: **147 tests** (92 unit + 40 integration + 15 architecture)
     - **Total passing**: ~130 tests (88%)
     - Coverage reports available in BookingSystem.UnitTests/TestResults/CoverageReport/index.html

## Day 8: Frontend Setup (React + TypeScript + Vite)

### Manual Vite Project Creation

**Note**: Vite CLI `create-vite` required interactive prompts that couldn't be automated. Proceeded with manual project setup.

355. Created client folder and initialized project:
     - `mkdir client; cd client`
     - `npm init -y`

356. Installed React core dependencies:
     - `npm install react react-dom`
     - React v19.2.4, React-DOM v19.2.4

357. Installed Vite and TypeScript tooling:
     - `npm install -D vite @vitejs/plugin-react typescript @types/react @types/react-dom`
     - Vite v8.0.5, TypeScript v6.0.2

358. Updated package.json:
     - Changed `"type"` from "commonjs" to "module"
     - Added scripts: `dev` (vite), `build` (tsc && vite build), `preview` (vite preview)
     - Added `"private": true`

359. Created vite.config.ts:
     - React plugin: @vitejs/plugin-react
     - Path alias: @ → ./src
     - Dev server: port 3000
     - Proxy: /api → http://localhost:5036 (backend API)

360. Created tsconfig.json:
     - Target: ES2020
     - Module: ESNext
     - JSX: react-jsx (automatic runtime)
     - Strict mode: enabled
     - Path mapping: @/_ → ./src/_

361. Created tsconfig.node.json:
     - TypeScript config for Vite config file
     - Module resolution: bundler

362. Created index.html (entry point):
     - HTML5 structure
     - div#root for React mounting
     - script tag: type="module" src="/src/main.tsx"

### TailwindCSS Setup

363. Installed TailwindCSS and PostCSS:
     - `npm install -D tailwindcss postcss autoprefixer`
     - TailwindCSS v4.x, PostCSS, Autoprefixer

364. Created tailwind.config.js:
     - Dark mode: class-based
     - Content: index.html and src/\*_/_.{js,ts,jsx,tsx}
     - Theme extensions: CSS variables for border-radius (shadcn/ui compatible)

365. Created postcss.config.js:
     - Plugins: tailwindcss, autoprefixer

366. Created src/index.css:
     - TailwindCSS directives: @tailwind base, components, utilities
     - CSS custom properties: --radius

### React Source Files

367. Created src/main.tsx:
     - React 18+ entry point with createRoot API
     - StrictMode wrapper
     - Imports: React, ReactDOM, index.css, App

368. Created src/App.tsx:
     - Root functional component
     - Basic UI: "Booking System" heading with TailwindCSS classes
     - Verified TailwindCSS working (bg-gray-50, text-4xl, etc.)

369. Created src/vite-env.d.ts:
     - TypeScript reference types for Vite client

### Core Libraries Installation

370. Installed Redux Toolkit and React Router:
     - `npm install @reduxjs/toolkit react-redux react-router-dom axios`
     - @reduxjs/toolkit v2.x (state management)
     - react-redux v9.x (React bindings for Redux)
     - react-router-dom v7.x (routing)
     - axios v1.x (HTTP client)

371. Installed shadcn/ui peer dependencies:
     - `npm install class-variance-authority clsx tailwind-merge lucide-react`
     - class-variance-authority: variant-based styling
     - clsx + tailwind-merge: className utility
     - lucide-react: icon library

372. Created src/lib/utils.ts:
     - cn() utility function for combining class names with clsx and tailwind-merge
     - Used by shadcn/ui components

### Project Structure

373. Created folder structure:
     - src/features/ (auth, resources, bookings)
     - src/components/ (shared UI components)
     - src/store/ (Redux store configuration)
     - src/types/ (TypeScript interfaces)
     - src/lib/ (utilities)

374. Created .gitignore for client:
     - node_modules, dist, dist-ssr
     - logs (\*.log)
     - editor files (.vscode, .idea, .DS_Store)
     - \*.local

### Verification

375. Started Vite dev server:
     - Command: `npm run dev`
     - Server running on http://localhost:3000

376. **Package Summary**:
     - Total packages: 77 audited
     - Vulnerabilities: 0
     - Key dependencies:
       - React v19.2.4
       - Vite v8.0.5
       - TypeScript v6.0.2
       - TailwindCSS v4.x
       - Redux Toolkit v2.x
       - React Router v7.x
       - Axios v1.x

---

## Day 9: Authentication Flow & Tenant Setup

### Auth Redux Slice - Step 1

377. Created TypeScript auth types in `src/types/auth.types.ts`
378. Created Redux store configuration in `src/store/store.ts`:
     - Configured `configureStore` with auth reducer
     - Added middleware for serializable check (ignored persist actions)
     - Exported `RootState` type: `ReturnType<typeof store.getState>`
     - Exported `AppDispatch` type: `typeof store.dispatch`

379. Created typed Redux hooks in `src/store/hooks.ts`
380. Created auth Redux slice in `src/features/auth/authSlice.ts`

### Token Storage Utilities - Step 2

381. Created token storage utilities in `src/lib/tokenStorage.ts`

### Axios Configuration with Interceptors - Step 3

382. Created axios instance with interceptors in `src/lib/apiClient.ts`:
     - **Base configuration**: baseURL: '/api/v1' (uses Vite proxy to backend)
     - **Request interceptor**: Automatically adds headers to every request:
       - `Authorization: Bearer <accessToken>` (from localStorage)
       - `X-Tenant-Id: <tenantId>` (required by backend TenantResolutionMiddleware)
     - **Response interceptor**: Handles 401 Unauthorized errors:
       - Detects expired access token
       - Calls `/api/v1/auth/refresh` with refresh token
       - Updates tokens in localStorage
       - Retries original request with new access token
       - On refresh failure: clears auth data, redirects to `/login`
     - **Token refresh queue**: Prevents duplicate refresh requests
       - `isRefreshing` flag ensures only one refresh call at a time
       - `failedQueue` holds concurrent 401 requests
       - After refresh success, all queued requests retry automatically
     - Purpose: Seamless token management, no manual header injection needed

### Protected Route Component - Step 4

383. Created ProtectedRoute component in `src/components/ProtectedRoute.tsx`:
     - Wraps protected pages/routes that require authentication
     - Uses `useAppSelector` to check Redux auth state (`isAuthenticated`, `isLoading`)
     - **Three states**:
       - Loading: Shows "Loading..." message while checking auth
       - Not authenticated: Redirects to `/login` with `<Navigate replace />`
       - Authenticated: Renders `{children}` (protected content)
     - `replace` prop: Prevents adding redirect to browser history
     - Purpose: Centralized route protection, prevents unauthorized access
     - Usage: `<ProtectedRoute><DashboardPage /></ProtectedRoute>`
