# Day 7: Backend Testing

**Steps: 1-71**

---

## Backend Testing - Step 1: Set up xUnit test projects

1. Created xUnit test project for unit tests
2. Created xUnit test project for integration tests
3. Added test projects to solution
4. Configured project references for UnitTests
5. Configured project references for IntegrationTests
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
15. Executed unit tests

## Backend Testing - Step 4: Write unit tests for CQRS Command Handlers

16. Created `CreateResourceCommandHandlerTests` in `BookingSystem.UnitTests/Application/Commands`
17. Created `CreateBookingCommandHandlerTests` in `BookingSystem.UnitTests/Application/Commands`
18. Executed unit tests

## Backend Testing - Step 5: Write unit tests for FluentValidation Validators

19. Created `CreateResourceCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
20. Created `CreateBookingCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
21. Created `LoginCommandValidatorTests` in `BookingSystem.UnitTests/Application/Validators`
22. Test run

## Backend Testing - Step 6: Set up Integration test infrastructure (WebApplicationFactory)

23. Made Program class accessible to integration tests
24. Created `IntegrationTestWebApplicationFactory` in `BookingSystem.IntegrationTests`
25. Created `IntegrationTestBase`
26. Created `InfrastructureSmokeTests`
27. Executed integration smoke tests

## Backend Testing - Step 7: Configure Testcontainers for PostgreSQL

28. Created `DatabaseFixture` in `BookingSystem.IntegrationTests/Infrastructure`:
29. Created `TestWebApplicationFactory` in `BookingSystem.IntegrationTests/Infrastructure`
30. Updated `IntegrationTestBase`:
31. Updated `InfrastructureSmokeTests`:
32. Executed integration tests

33. Created DatabaseFixture with PostgreSqlContainer lifecycle management
34. Created TestWebApplicationFactory with database connection injection
35. Updated IntegrationTestBase to use DatabaseFixture,

## Backend Testing - Step 8: Write integration tests for Auth endpoints

34. Created AuthControllerTests in BookingSystem.IntegrationTests/Controllers
35. Updated GlobalExceptionHandlerMiddleware to map UnauthorizedAccessException → 401, ArgumentException → 400
36. Updated RegisterTenantCommandHandler to generate and store RefreshToken
37. Updated RegisterUserCommandHandler to generate and store RefreshToken
38. Fixed IntegrationTestBase helper methods to return full DTO objects (RegisterTenantResponse, LoginResponse)
39. Test execution

## Step 9: Resources CRUD Integration Tests

**Goal**: Write comprehensive integration tests for Resources endpoints with authentication, multi-tenant isolation, pagination, and database verification.

40. Created ResourcesControllerTests in BookingSystem.IntegrationTests/Controllers with 12 tests:
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

41. Updated TestWebApplicationFactory to explicitly override JWT validation

## Step 10: Bookings Integration Tests

**Goal**: Write comprehensive integration tests for Bookings endpoints including conflict detection, status workflows, and authorization.

52. Created BookingsControllerTests in BookingSystem.IntegrationTests/Controllers with 11 tests:
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

53. Added `InvalidOperationException`

## Step 11: Architecture Tests (NetArchTest.Rules)

**Goal**: Validate Clean Architecture principles and enforce coding conventions using automated architecture tests.

58. Installed NetArchTest.Rules package: `dotnet add package NetArchTest.Rules` (version 1.3.2)
59. Created AssemblyReference
60. Created ArchitectureTests in BookingSystem.UnitTests/Architecture with 15 comprehensive tests:

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

61. Added project references to UnitTests
62. Test execution

## Step 12: Code Coverage Measurement

**Goal**: Measure code coverage from unit and integration tests, generate reports, and document coverage by layer.

65. Installed coverlet.msbuild for code coverage collection
66. Ran unit tests with coverage collection:
67. Installed ReportGenerator global tool for HTML coverage reports Version: 5.5.4
68. Generated HTML coverage report from unit tests
69. **Unit Test Coverage Statistics** (by layer):
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

70. Attempted integration test coverage measurement
71. **Coverage Analysis Summary**:
     - Unit tests provide **accurate coverage** for Domain (64%) and Application (12%) layers
     - Integration tests validate **end-to-end functionality** but don't contribute to coverage metrics reliably
     - Combined test suite: **147 tests** (92 unit + 40 integration + 15 architecture)
     - **Total passing**: ~130 tests (88%)
     - Coverage reports available in BookingSystem.UnitTests/TestResults/CoverageReport/index.html
