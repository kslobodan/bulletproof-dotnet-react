# Day 2: Core Infrastructure & Multi-tenancy

**Steps: 1-44**

---

## Middleware

1. Created `GlobalExceptionHandlerMiddleware.cs` in API/Middleware
2. Registered middleware in `Program.cs`

## API Versioning

3. Installed API versioning packages:
    - `dotnet add package Asp.Versioning.Http`
    - `dotnet add package Asp.Versioning.Mvc.ApiExplorer`
4. Configured API versioning in `Program.cs` (default v1.0)
5. Added Controllers support with `AddControllers()` and `MapControllers()`

## Swagger/OpenAPI

6. Installed Swashbuckle: `dotnet add package Swashbuckle.AspNetCore`
7. Configured Swagger in `Program.cs` with API documentation

## Common Patterns

8. Created `PagedResult<T>` class in Application/Common/Models for pagination

## Database Migrations

9. Installed DbUp: `dotnet add package dbup-postgresql` in Infrastructure project
10. Created `DatabaseMigration.cs` class with migration runner
11. Created initial schema SQL script: `0001_InitialSchema.sql` (Tenants, Users, Roles, UserRoles)
12. Configured .csproj to embed SQL scripts as resources
13. Integrated migration runner in `Program.cs` to run on startup

## Repository Pattern

14. Created `IDbConnectionFactory` interface in Application layer
15. Implemented `DbConnectionFactory` in Infrastructure using Npgsql
16. Registered factory in DI container as singleton
17. Fixed package conflict: Removed `Microsoft.AspNetCore.OpenApi` (conflicted with Swashbuckle 10.1.7)
18. Verified application startup: `dotnet run --project ...\bulletproof-dotnet-react\src\BookingSystem.API\BookingSystem.API.csproj`
19. Database migrations executed successfully, all tables created
20. Verified database schema with `docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "\dt"`
1. Verified seed data: 3 roles (TenantAdmin, Manager, User) inserted successfully
2. Updated INSTALLATION_GUIDE.md to reflect working package configuration

## Multi-Tenancy (TenantContext Service)

3. Created `ITenantContext` interface in Application/Common/Interfaces
4. Implemented `TenantContext` service in Infrastructure/Services with SetTenantId() and Clear() methods
5. Created `TenantResolutionMiddleware` in API/Middleware to extract tenant from X-Tenant-Id header
6. Registered TenantContext as scoped service in DI container
7. Added TenantResolutionMiddleware to pipeline (after global error handling, before controllers)
8. Middleware skips Swagger and health check endpoints
9. Middleware validates X-Tenant-Id header format (GUID) and returns 400 if invalid or missing
10. Created test controller `TenantController` (v1) to verify tenant resolution
11. Tested middleware:
    - ✅ Request without header: 400 "X-Tenant-Id header is required"
    - ✅ Request with invalid GUID: 400 "Invalid X-Tenant-Id header format"
    - ✅ Request with valid GUID: 200 with tenantId and isResolved=true
    - ✅ Swagger endpoint bypasses tenant check

## Multi-Tenant Query Filter (Repository Pattern with Dapper)

12. Installed Dapper in Application project for extension methods
13. Created `DapperExtensions.cs` with tenant-aware query methods:
    - `QueryWithTenantAsync<T>()` - SELECT with automatic TenantId filtering
    - `QuerySingleOrDefaultWithTenantAsync<T>()` - Single result with tenant filter
    - `ExecuteWithTenantAsync()` - INSERT/UPDATE/DELETE with tenant filter
14. Created `IRepository<T>` base interface in Application/Common/Interfaces
15. Created `BaseRepository<T>` abstract class in Infrastructure/Repositories with:
    - Automatic TenantId injection in all queries
    - GetByIdAsync, GetAllAsync, DeleteAsync, ExistsAsync with tenant filtering
    - Abstract AddAsync and UpdateAsync for entity-specific implementation
16. Created `User` entity in Domain/Entities with UUID Id and TenantId
17. Created `IUserRepository` interface extending IRepository<User>
18. Implemented `UserRepository` with tenant-aware CRUD operations
19. Registered `IUserRepository` in DI container as scoped service
20. Created migration `0002_ConvertToUUID.sql` to convert all ID columns from SERIAL to UUID
1. Migration drops and recreates tables with `gen_random_uuid()` for PostgreSQL UUID support
2. Created `UsersController` (v1) to demonstrate multi-tenant filtering
3. Created test tenants in database (aaaaa... and bbbbb...)
4. Tested multi-tenant data isolation:
    - ✅ Created user for Tenant 1 (john@tenant1.com)
    - ✅ Created user for Tenant 2 (jane@tenant2.com)
    - ✅ Tenant 1 query returns only Tenant 1 users
    - ✅ Tenant 2 query returns only Tenant 2 users
    - ✅ Cross-tenant access blocked (Tenant 2 cannot access Tenant 1's user - returns 404)
    - ✅ Same email allowed in different tenants (john@tenant1.com exists in both tenants)
    - ✅ Multi-tenant data isolation fully functional
