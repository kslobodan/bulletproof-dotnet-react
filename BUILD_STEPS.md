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
17. Configured MediatR in `Program.cs` (registered handlers from Application assembly)
18. Added PostgreSQL connection string to `appsettings.Development.json`
19. Configured FluentValidation in `Program.cs`
20. Configured AutoMapper in `Program.cs`

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
38. Verified application startup: `dotnet run --project D:\Posao\bulletproof-dotnet-react\src\BookingSystem.API\BookingSystem.API.csproj`
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

153. In `Infrastructure/Repositories/BookingRepository.cs` created `BookingRepository`
