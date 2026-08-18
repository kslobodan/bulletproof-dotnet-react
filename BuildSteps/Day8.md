# Day 8: Backend Testing

---

## Backend Testing - Step 1: Set up xUnit test projects

1. Created xUnit test project for unit tests: `dotnet new xunit -n BookingSystem.UnitTests -o BookingSystem.UnitTests`
2. Created xUnit test project for integration tests: `dotnet new xunit -n BookingSystem.IntegrationTests -o BookingSystem.IntegrationTests`
3. Added test projects to solution:
   - `dotnet sln BookingSystem.sln add BookingSystem.UnitTests/BookingSystem.UnitTests.csproj`,
   - `dotnet sln BookingSystem.sln add BookingSystem.IntegrationTests/BookingSystem.IntegrationTests.csproj`
4. Configured project references for UnitTests:
   - `dotnet add BookingSystem.UnitTests reference BookingSystem.Domain/BookingSystem.Domain.csproj`,
   - `dotnet add BookingSystem.UnitTests reference BookingSystem.Application/BookingSystem.Application.csproj`
5. Configured project references for IntegrationTests:
   - `dotnet add BookingSystem.IntegrationTests reference BookingSystem.API/BookingSystem.API.csproj`,
   - `dotnet add BookingSystem.IntegrationTests reference BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj`
6. Deleted default test files:
   - `Remove-Item BookingSystem.UnitTests/UnitTest1.cs -Force`
   - `Remove-Item BookingSystem.IntegrationTests/UnitTest1.cs -Force`
7. Created folder structure in UnitTests project:
   - `Domain/Entities`
   - `Application/Commands`
   - `Application/Queries`
   - `Application/Validators`
8. Created folder structure in IntegrationTests project:
   - `Controllers`
   - `Infrastructure`
9. Verified build: Test projects compiled successfully ✅

## Backend Testing - Step 2: Install testing NuGet packages

10. Installed testing packages for UnitTests project:
    - `dotnet add package Moq` → version 4.20.72
    - `dotnet add package FluentAssertions` → version 8.9.0
11. Installed testing packages for IntegrationTests project:
    - `dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.*` → version 9.0.14 (WebApplicationFactory for in-memory testing)
    - `dotnet add package Testcontainers.PostgreSql` → version 4.11.0 (PostgreSQL container for integration tests)
    - `dotnet add package FluentAssertions` → version 8.9.0 (readable assertions)

## Backend Testing - Step 3: Write unit tests for Domain entities

12. Created `RefreshTokenTests` with 9 test methods
13. Created `BookingTests` with 7 test methods
14. Created `AvailabilityRuleTests`
15. Executed unit tests: `dotnet test BookingSystem.UnitTests/BookingSystem.UnitTests.csproj`

## Backend Testing - Step 4: Write unit tests for CQRS Command Handlers

16. Created `CreateResourceCommandHandlerTests` in `BookingSystem.UnitTests/Application/Commands`
17. Created `CreateBookingCommandHandlerTests` in `BookingSystem.UnitTests/Application/Commands`
18. Executed unit tests: `dotnet test BookingSystem.UnitTests/BookingSystem.UnitTests.csproj`

## Backend Testing - Step 5: Write unit tests for FluentValidation Validators

19. Created `CreateResourceCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
20. Created `CreateBookingCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
21. Created `LoginCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
22. Executed unit tests: `dotnet test BookingSystem.UnitTests/BookingSystem.UnitTests.csproj`

## Backend Testing - Step 6: Set up Integration test infrastructure (WebApplicationFactory)

23. Made Program class accessible to integration tests by adding at the end of `Program.cs`:
    `public partial class Program { }`
    **Why?** In .NET 6+, the `Program` class is implicitly internal. `WebApplicationFactory<T>` needs a public entry point class to bootstrap the application in tests.
24. Created `IntegrationTestWebApplicationFactory` in `BookingSystem.IntegrationTests`
25. Created `IntegrationTestBase` in `BookingSystem.IntegrationTests`
26. Created `InfrastructureSmokeTests` in `BookingSystem.IntegrationTests`
27. Executed integration smoke tests: `dotnet test BookingSystem.IntegrationTests/BookingSystem.IntegrationTests.csproj`

## Backend Testing - Step 7: Configure Testcontainers for PostgreSQL

28. Created `DatabaseFixture` in `BookingSystem.IntegrationTests/Infrastructure`
29. Created `TestWebApplicationFactory` in `BookingSystem.IntegrationTests/Infrastructure`:
    - Replaces `IntegrationTestWebApplicationFactory` from Step 6
    - Injects Testcontainer database connection
    - Applies migrations automatically

## Backend Testing - Step 8: Write integration tests for Auth endpoints

30. Created `AuthControllerTests` in `BookingSystem.IntegrationTests/Controllers`
31. Updated `GlobalExceptionHandlerMiddleware` to map UnauthorizedAccessException → 401, ArgumentException → 400
32. Updated `RegisterTenantCommandHandler` to generate and store RefreshToken
33. Updated `RegisterUserCommandHandler` to generate and store RefreshToken
34. Executed auth integration tests: `dotnet test BookingSystem.IntegrationTests/BookingSystem.IntegrationTests.csproj --filter "FullyQualifiedName~AuthControllerTests"`
    Result: All auth endpoint tests passed (Register, Login, RefreshToken, MultiTenant) ✅

## Step 9: Resources CRUD Integration Tests

35. Created `ResourcesControllerTests` in `BookingSystem.IntegrationTests/Controllers`

## Step 10: Bookings Integration Tests

36. Created `BookingsControllerTests` in `BookingSystem.IntegrationTests/Controllers with 11 tests`

## Step 11: Architecture Tests (NetArchTest.Rules)

37. Installed NetArchTest.Rules package in `BookingSystem.UnitTests`: `dotnet add package NetArchTest.Rules` (version 1.3.2)

38. Created `AssemblyReference.cs` marker classes in each project layer (if not already present):
    - `BookingSystem.Domain/AssemblyReference.cs`
    - `BookingSystem.Application/AssemblyReference.cs`
    - `BookingSystem.Infrastructure/AssemblyReference.cs`
    - `BookingSystem.API/AssemblyReference.cs`

    **Purpose**: Architecture tests use these to get assembly references: `typeof(BookingSystem.Domain.AssemblyReference).Assembly`

39. Created `ArchitectureTests` in `BookingSystem.UnitTests/Architecture`
40. Added additional project references to UnitTests for architecture testing:
    - `dotnet add BookingSystem.UnitTests reference BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj`
    - `dotnet add BookingSystem.UnitTests reference BookingSystem.API/BookingSystem.API.csproj`

    **Why?** Architecture tests need access to ALL layer assemblies (Domain, Application, Infrastructure, API) to validate dependencies and naming conventions.

41. Executed architecture tests:
    `dotnet test BookingSystem.UnitTests/BookingSystem.UnitTests.csproj --filter "FullyQualifiedName~ArchitectureTests"`

## Step 12: Code Coverage Measurement

**Goal**: Measure code coverage from unit and integration tests, generate reports, and document coverage by layer.

42. Installed coverlet.msbuild for code coverage collection: `dotnet add BookingSystem.UnitTests package coverlet.msbuild`
43. Ran unit tests with coverage collection:
    ```powershell
    dotnet test BookingSystem.UnitTests/BookingSystem.UnitTests.csproj `
      /p:CollectCoverage=true `
      /p:CoverletOutputFormat=cobertura `
      /p:CoverletOutput=./TestResults/coverage.cobertura.xml
    ```
44. Installed ReportGenerator global tool for HTML coverage reports:
    `dotnet tool install -g dotnet-reportgenerator-globaltool` Version: 5.5.4
45. Generated HTML coverage report from unit tests:

    ```powershell
    reportgenerator `
      -reports:BookingSystem.UnitTests/TestResults/coverage.cobertura.xml `
      -targetdir:BookingSystem.UnitTests/TestResults/CoverageReport `
      -reporttypes:Html
    ```

    Open report: `BookingSystem.UnitTests/TestResults/CoverageReport/index.html`

46. **Unit Test Coverage Statistics** (by layer):
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

47. Attempted integration test coverage measurement
48. **Coverage Analysis Summary**:
    - Unit tests provide **accurate coverage** for Domain (64%) and Application (12%) layers
    - Integration tests validate **end-to-end functionality** but don't contribute to coverage metrics reliably
    - Combined test suite: **147 tests** (92 unit + 40 integration + 15 architecture)
    - **Total passing**: ~130 tests (88%)
    - Coverage reports available in BookingSystem.UnitTests/TestResults/CoverageReport/index.html
