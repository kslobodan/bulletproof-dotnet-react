# Day 3: Authentication & Authorization

**Steps: 1-38**

---

## Password Hashing Setup

1. Installed BCrypt.Net-Next package in Infrastructure project:
   - `cd src/BookingSystem.Infrastructure`
   - `dotnet add package BCrypt.Net-Next --version 4.0.3`
   - Package added to `BookingSystem.Infrastructure.csproj`
2. Created `IPasswordHasher` interface in `Application/Common/Interfaces/IPasswordHasher.cs`:
3. Implemented `PasswordHasher` service in `Infrastructure/Services/PasswordHasher.cs`:
   - Uses BCrypt.Net.BCrypt for hashing (adaptive algorithm with built-in salt)
4. Registered `IPasswordHasher` service in DI container (`Program.cs`):
   - `builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();`
   - Scoped lifetime: new instance per HTTP request

## JWT Token Generation

5. Installed JWT packages:
   - API project: `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.1` - Infrastructure project: `dotnet add package System.IdentityModel.Tokens.Jwt` (version 8.17.0 installed)
6. Added JWT configuration to `appsettings.json`:
7. Added JWT configuration to `appsettings.Development.json`
8. Created `IJwtTokenService` interface in `Application/Common/Interfaces/IJwtTokenService.cs`:`
9. Implemented `JwtTokenService` in `Infrastructure/Services/JwtTokenService.cs`:
   - Uses `System.IdentityModel.Tokens.Jwt` for token generation
   - Creates claims: NameIdentifier (userId), Email, Jti (unique token ID), and Role claims
   - Signs token with HMAC SHA256 using symmetric key from configuration
   - Token expiration based on configuration (default 60 minutes)
10. Registered `IJwtTokenService` in DI container (`Program.cs`)
11. Build verification: `dotnet build` - successful (6.0s)

## Tenant & User Entities for Authentication

12. Created `Tenant` entity in `Domain/Entities/Tenant.cs`
13. Verified `User` entity already has authentication fields
14. Created `ITenantRepository` interface in `Application/Common/Interfaces/ITenantRepository.cs`
15. Implemented `TenantRepository` in `Infrastructure/Repositories/TenantRepository.cs`:
16. Registered `ITenantRepository` in DI container (`Program.cs`)

## Authentication DTOs (Data Transfer Objects)

17. Created `AuthResult` DTO in `Application/Features/Authentication/DTOs/AuthResult.cs`:
18. Created `RegisterTenantRequest` DTO
19. Created `RegisterTenantResponse` DTO
20. Created `RegisterUserRequest` DTO
21. Created `RegisterUserResponse` DTO
22. Created `LoginRequest` DTO
23. Created `LoginResponse` DTO

## RegisterTenant Command (CQRS)

24. Created `RegisterTenantCommand` in `Application/Features/Authentication/Commands/RegisterTenant/`
25. Created `RegisterTenantCommandHandler`
26. Created `RegisterTenantCommandValidator` (FluentValidation)

## RegisterUser Command (CQRS)

27. Created `RegisterUserCommand` in `Application/Features/Authentication/Commands/RegisterUser/`
28. Created `RegisterUserCommandHandler`
29. Created `RegisterUserCommandValidator` (FluentValidation)

## Login Command (CQRS)

30. Created `LoginCommand` in `Application/Features/Authentication/Commands/Login/`
31. Created `LoginCommandHandler`
32. Created `LoginCommandValidator` (FluentValidation)

## Authentication Controller (API Endpoints)

33. Created `AuthController` in `API/Controllers/v1/AuthController.cs`

## JWT Authentication Middleware

34. Configured JWT authentication in `Program.cs`
35. Added authentication middleware to pipeline:
    - `app.UseAuthentication()` before `app.UseAuthorization()`

## Authorization Policies & Protected Endpoints

36. Configured authorization policies in `Program.cs`
37. Added `[Authorize]` attribute to protected controllers
38. Added `[AllowAnonymous]` attribute to `AuthController`:
    - Allows unauthenticated access to register-tenant, register-user, login endpoints
    - Required because FallbackPolicy would otherwise block these endpoints
