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
91. Build verification: `dotnet build` - successful (3.0s)

### RegisterUser Command (CQRS)

92. Created `RegisterUserCommand` in `Application/Features/Authentication/Commands/RegisterUser/`
93. Created `RegisterUserCommandHandler`
94. Created `RegisterUserCommandValidator` (FluentValidation)
