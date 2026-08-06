# Day 2: Core Infrastructure & Multi-tenancy

---

## Middleware

1. Created `GlobalExceptionHandlerMiddleware.cs` in API/Middleware
2. Registered middleware in `Program.cs`

## API Versioning

3. Installed API versioning packages:
   - `cd ../BookingSystem.API`
   - `dotnet add package Asp.Versioning.Http`
   - `dotnet add package Asp.Versioning.Mvc`
   - `dotnet add package Asp.Versioning.Mvc.ApiExplorer`
4. Configured API versioning in `Program.cs` (default v1.0)
5. Added Controllers support with `AddControllers()` and `MapControllers()` in Program.cs

## Swagger/OpenAPI

6. Installed Swashbuckle: `dotnet add package Swashbuckle.AspNetCore` in BookingSystem.API
7. Configured Swagger in `Program.cs` with API documentation

## Common Patterns

8. Created `PagedResult<T>` class in Application/Common/Models for pagination

## Database Migrations

9. Installed DbUp: `dotnet add package dbup-postgresql` in Infrastructure project
10. Created `DatabaseMigration.cs` class with migration runner in BookingSystem.Infrastructure/Data
11. Created initial schema SQL script: `0001_InitialSchema.sql` with UUID-based IDs (Tenants, Users, Roles, UserRoles) in Infrastructure/Data/Scripts
12. Configured .csproj to embed SQL scripts as resources
13. Integrated migration runner in `Program.cs` to run on startup

## Repository Pattern

14. Created `IDbConnectionFactory` interface in Application/Common/Interfaces
15. Installed DbUp: `dotnet add package Microsoft.Extensions.Configuration.Abstractions` in Infrastructure project
16. Implemented `DbConnectionFactory` in Infrastructure using Npgsql in Infrastructure/Data
17. Registered factory in Program.cs as singleton
18. Fixed package conflict: Removed `Microsoft.AspNetCore.OpenApi` (conflicted with Swashbuckle 10.1.7)
19. Verified application startup:
    `dotnet run --project ...\bulletproof-dotnet-react\src\BookingSystem.API\BookingSystem.API.csproj`
20. Database migrations executed successfully, all tables created
21. Verified database schema with:
    `docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "\dt"`
22. Verified seed data: 3 roles (TenantAdmin, Manager, User) inserted successfully with:
    `docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT * FROM roles;"`

## Multi-Tenancy (TenantContext Service)

22. Created `ITenantContext` interface in Application/Common/Interfaces
23. Implemented `TenantContext` service in Infrastructure/Services with SetTenantId() and Clear() methods
24. Created `TenantResolutionMiddleware` in API/Middleware to extract tenant from X-Tenant-Id header
25. Registered TenantContext as scoped service in Program.cs
26. Added TenantResolutionMiddleware to pipeline (after global error handling, before controllers)
27. Created test controller `TenantController` (v1) to verify tenant resolution in API/Controllers/v1
28. Tested middleware (see [API_TESTS.md](../API_TESTS.md#test-tenant-resolution-middleware) for commands):
    - ✅ Request without header: 400 "X-Tenant-Id header is required"
    - ✅ Request with invalid GUID: 400 "Invalid X-Tenant-Id header format"
    - ✅ Request with valid GUID: 200 with tenantId and isResolved=true
    - ✅ Swagger endpoint bypasses tenant check

## Multi-Tenant Query Filter (Repository Pattern with Dapper)

31. Installed Dapper in Application project: `dotnet add package Dapper`
32. Created `DapperExtensions.cs` with tenant-aware query methods in Application/Common/Extensions
33. Created `IRepository<T>` base interface in Application/Common/Interfaces
34. Created `BaseRepository<T>` abstract class in Infrastructure/Repositories
35. Created `User` entity in Domain/Entities with UUID Id and TenantId
36. Created `IUserRepository` interface in Application/Common/Interfaces
37. Implemented `UserRepository` in Infrastructure/Repositories
38. Registered `IUserRepository` in DI container as scoped service
39. Created `UsersController` in API/Controllers/v1
40. Created test tenants in database:

    ```powershell
    # Create Tenant 1
    docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "INSERT INTO Tenants (Id, Name, Email, Plan, IsActive, CreatedAt) VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Tenant One', 'tenant1@example.com', 'Pro', true, CURRENT_TIMESTAMP);"

    # Create Tenant 2
    docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "INSERT INTO Tenants (Id, Name, Email, Plan, IsActive, CreatedAt) VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Tenant Two', 'tenant2@example.com', 'Pro', true, CURRENT_TIMESTAMP);"

    # Verify tenants created
    docker exec -it bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT id, name, email FROM Tenants;"
    ```

41. Tested multi-tenant data isolation (see [test-multitenant.http](../src/BookingSystem.API/test-multitenant.http) for full test suite):
    - ✅ Created user for Tenant 1 (john@tenant1.com)
    - ✅ Created user for Tenant 2 (jane@tenant2.com)
    - ✅ Tenant 1 query returns only Tenant 1 users
    - ✅ Tenant 2 query returns only Tenant 2 users
    - ✅ Cross-tenant access blocked (Tenant 2 cannot access Tenant 1's user - returns 404)
    - ✅ Same email allowed in different tenants (john@tenant1.com exists in both tenants)
    - ✅ Multi-tenant data isolation fully functional
