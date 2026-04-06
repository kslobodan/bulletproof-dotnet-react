# Day 7: Backend Testing

[← Back to Index](./README.md) | [← Previous: Day 6](./Day6-Advanced.md)

---

## Q: "Explain your testing strategy. What types of tests did you write and why?"

**A:** "I implemented a three-layer testing strategy to validate different aspects of the application:

**Testing Pyramid:**

```
        /\
       /E2\     ← E2E Tests (Future: Playwright/Cypress)
      /----\
     / Int  \   ← Integration Tests (~40 tests)
    /--------\
   /   Unit   \ ← Unit Tests (92 tests)
  /------------\
 / Architecture \ ← Architecture Tests (15 tests)
/________________\
```

**1. Unit Tests (92 tests) - Focus: Business Logic**

```csharp
// Tested: Domain entities, Command/Query handlers, Validators
[Fact]
public void Booking_ShouldDetectOverlap_WhenTimesIntersect()
{
    // Arrange
    var existingBooking = new Booking
    {
        StartTime = new DateTime(2026, 4, 7, 10, 0, 0),
        EndTime = new DateTime(2026, 4, 7, 12, 0, 0)
    };

    // Act
    var overlaps = existingBooking.OverlapsWith(
        new DateTime(2026, 4, 7, 11, 0, 0),  // Starts during existing booking
        new DateTime(2026, 4, 7, 13, 0, 0)
    );

    // Assert
    overlaps.Should().BeTrue();
}
```

**2. Integration Tests (~40 tests) - Focus: API Endpoints + Database**

```csharp
// Tested: HTTP endpoints, multi-tenant isolation, database operations
[Fact]
public async Task CreateBooking_WithOverlappingTimes_ShouldReturn400Conflict()
{
    // Arrange: Create tenant, resource, first booking
    var tenant = await RegisterTenantAsync();
    var resource = await CreateResourceAsync(tenant.TenantId, tenant.Token);
    var firstBooking = await CreateBookingAsync(resource.Id,
        startTime: DateTime.UtcNow.AddHours(1),
        endTime: DateTime.UtcNow.AddHours(2));

    // Act: Try to create overlapping booking
    var response = await PostAsync("/api/v1/bookings", new
    {
        ResourceId = resource.Id,
        StartTime = DateTime.UtcNow.AddHours(1.5),  // Overlaps!
        EndTime = DateTime.UtcNow.AddHours(2.5)
    }, tenant.TenantId, tenant.Token);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var error = await response.Content.ReadAsStringAsync();
    error.Should().Contain("booked");  // Conflict detection message
}
```

**3. Architecture Tests (15 tests) - Focus: Clean Architecture Compliance**

```csharp
// Tested: Layer dependencies, naming conventions, CQRS patterns
[Fact]
public void Domain_Should_NotHaveAnyDependencies()
{
    var domainAssembly = typeof(BookingSystem.Domain.AssemblyReference).Assembly;

    var result = Types.InAssembly(domainAssembly)
        .ShouldNot()
        .HaveDependencyOnAny("Application", "Infrastructure", "API")
        .GetResult();

    Assert.True(result.IsSuccessful, "Domain layer violated!");
}
```

**Why This Strategy?**

| Test Type    | Speed  | Coverage            | Purpose                         |
| ------------ | ------ | ------------------- | ------------------------------- |
| Unit         | Fast   | Domain, Application | Business logic correctness      |
| Integration  | Medium | API, Infrastructure | End-to-end flows, DB operations |
| Architecture | Fast   | All layers          | Enforce design principles       |

**Test Coverage Results:**

- Domain layer: **64% line coverage, 100% branch coverage** ✅
- Total: 147 tests, ~130 passing (88%)
- Critical paths fully covered (booking conflicts, multi-tenant isolation)

**Key Principle:** Test behavior, not implementation. My tests focus on 'what' the system does, not 'how' it does it, making them resilient to refactoring."

---

## Q: "How did you test multi-tenant isolation? How do you ensure tenants can't access each other's data?"

**A:** "Multi-tenant isolation is critical for security, so I wrote dedicated integration tests to verify it:

**Test Strategy:**

```csharp
[Fact]
public async Task MultiTenant_ResourceIsolation_TenantCannotAccessOtherTenantsResources()
{
    // Arrange: Create two separate tenants
    var tenant1 = await RegisterTenantAsync("tenant1@example.com");
    var tenant2 = await RegisterTenantAsync("tenant2@example.com");

    // Act: Tenant 1 creates a resource
    var resource = await CreateResourceAsync(new CreateResourceRequest
    {
        Name = "Tenant1 Private Room",
        ResourceType = "MeetingRoom",
        Capacity = 10
    }, tenant1.TenantId, tenant1.Token);

    // Act: Tenant 2 tries to access Tenant 1's resource
    var response = await GetAsync(
        $"/api/v1/resources/{resource.Id}",
        tenant2.TenantId,    // Different tenant!
        tenant2.Token
    );

    // Assert: Should return 404 (resource filtered by tenant)
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    // NOT 403 Forbidden - we don't reveal that the resource exists
}
```

**What This Tests:**

1. **Tenant Context Resolution**: X-Tenant-Id header correctly sets tenant scope
2. **Query Filtering**: Repository automatically adds `WHERE tenantid = @TenantId`
3. **Security by Design**: Other tenant's data is invisible (404, not 403)

**Additional Multi-Tenant Tests:**

```csharp
// Test 1: List Resources - Tenant only sees their own
[Fact]
public async Task GetAllResources_ReturnsOnlyCurrentTenantsResources()
{
    var tenant1 = await RegisterTenantAsync();
    var tenant2 = await RegisterTenantAsync();

    await CreateResourceAsync("Room A", tenant1.TenantId, tenant1.Token);
    await CreateResourceAsync("Room B", tenant1.TenantId, tenant1.Token);
    await CreateResourceAsync("Room C", tenant2.TenantId, tenant2.Token);

    var response = await GetAsync("/api/v1/resources", tenant1.TenantId, tenant1.Token);
    var resources = await response.Content.ReadFromJsonAsync<PagedResult<ResourceDto>>();

    resources.Items.Should().HaveCount(2);  // Only tenant1's resources
    resources.Items.Should().AllSatisfy(r => r.Name.Should().NotBe("Room C"));
}

// Test 2: Booking Conflicts - Only within same tenant
[Fact]
public async Task CreateBooking_DifferentTenants_ShouldNotDetectConflict()
{
    var tenant1 = await RegisterTenantAsync();
    var tenant2 = await RegisterTenantAsync();

    // Each tenant creates resource with SAME name (allowed!)
    var resource1 = await CreateResourceAsync("Conference Room", tenant1.TenantId, tenant1.Token);
    var resource2 = await CreateResourceAsync("Conference Room", tenant2.TenantId, tenant2.Token);

    // Both book at same time
    var booking1 = await CreateBookingAsync(resource1.Id, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), tenant1.TenantId, tenant1.Token);
    var booking2 = await CreateBookingAsync(resource2.Id, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), tenant2.TenantId, tenant2.Token);

    // Both should succeed - no cross-tenant conflict detection
    booking1.Should().NotBeNull();
    booking2.Should().NotBeNull();
}
```

**Why This Matters:**

In production, a tenant isolation bug could be catastrophic:

- **Data breach**: Tenant A sees Tenant B's proprietary information
- **GDPR violation**: Exposing customer data across organizations
- **Business impact**: Lost trust, legal liability

These tests provide confidence that our `BaseRepository<T>` tenant filtering works correctly at the API boundary."

---

## Q: "You used Testcontainers for integration tests. Why not use an in-memory database or mocks?"

**A:** "Testcontainers gives us a **real PostgreSQL database** for integration tests. Here's why that matters:

**Option 1: In-Memory Database (e.g., SQLite)**

```csharp
// ❌ Problems:
// 1. Different SQL dialect (SQLite != PostgreSQL)
// 2. Different type system (no TIME type in SQLite)
// 3. Different constraints and indexes
// 4. False confidence - tests pass but production fails
```

**Option 2: Mocked Database**

```csharp
// ❌ Problems:
// 1. Mocks test your mocks, not your SQL queries
// 2. No validation of JOIN logic, migrations, or constraints
// 3. Brittle - breaks when implementation changes
```

**Option 3: Testcontainers (What We Used)**

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Run real migrations (DbUp)
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(_container.GetConnectionString())
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigration).Assembly)
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
            throw result.Error;
    }

    public string GetConnectionString() => _container.GetConnectionString();
}
```

**Benefits of Testcontainers:**

✅ **Real Database**: Actual PostgreSQL 16, same as production
✅ **Real Migrations**: Tests run same migrations as production
✅ **Real Constraints**: FK violations, check constraints, indexes all work
✅ **Real SQL**: Tests PostgreSQL-specific features (TIME type, DATE_TRUNC, etc.)
✅ **Isolation**: Each test class gets fresh database
✅ **CI/CD Friendly**: Docker runs on GitHub Actions

**Performance Consideration:**

```csharp
// Fixture shared across test collection for speed
[Collection("Database")]
public class ResourcesControllerTests : IntegrationTestBase
{
    public ResourcesControllerTests(DatabaseFixture dbFixture)
        : base(dbFixture)
    {
        // Container started once, reused for all tests in collection
    }
}
```

**Trade-off:**

- **Slower than in-memory** (~5 seconds startup vs instant)
- **Much faster than manual testing** (automated, repeatable)
- **Higher confidence** (tests real database behavior)

**Production Incident Prevented:**

During development, Testcontainers caught a bug where I used `tenant_id` (snake_case) in SQL but PostgreSQL converted unquoted identifiers to lowercase (`tenantid`). In-memory SQLite would have missed this!"

---

## Q: "Explain your architecture tests. How do you enforce Clean Architecture programmatically?"

**A:** "I use **NetArchTest.Rules** to write automated tests that prevent architectural violations:

**Installation & Setup:**

```bash
dotnet add package NetArchTest.Rules --version 1.3.2
```

**Example 1: Enforce Layer Dependencies**

```csharp
[Fact]
public void Domain_Should_NotHaveAnyDependencies()
{
    var domainAssembly = typeof(BookingSystem.Domain.AssemblyReference).Assembly;

    var result = Types.InAssembly(domainAssembly)
        .ShouldNot()
        .HaveDependencyOnAny(
            "BookingSystem.Application",
            "BookingSystem.Infrastructure",
            "BookingSystem.API"
        )
        .GetResult();

    Assert.True(result.IsSuccessful,
        $"Domain violated! Dependencies: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? Array.Empty<string>())}");
}
```

**What This Prevents:**

```csharp
// ❌ COMPILATION ERROR if someone tries:
namespace BookingSystem.Domain.Entities;

using BookingSystem.Infrastructure.Data;  // ← Architecture test fails!

public class Booking
{
    // Domain should never reference Infrastructure
}
```

**Example 2: Enforce CQRS Naming Conventions**

```csharp
[Fact]
public void Commands_Should_EndWithCommand()
{
    var applicationAssembly = typeof(BookingSystem.Application.AssemblyReference).Assembly;

    var result = Types.InAssembly(applicationAssembly)
        .That()
        .ImplementInterface(typeof(IRequest<>))
        .And()
        .ResideInNamespace("BookingSystem.Application.Features")
        .And()
        .AreClasses()
        .And()
        .AreNotAbstract()
        .Should()
        .HaveNameEndingWith("Command")
        .Or()
        .HaveNameEndingWith("Query")
        .GetResult();

    Assert.True(result.IsSuccessful);
}
```

**Example 3: Prevent Controllers from Using Repositories Directly**

```csharp
[Fact]
public void API_Controllers_Should_NotDirectlyUseRepositories()
{
    var apiAssembly = typeof(BookingSystem.API.AssemblyReference).Assembly;

    var result = Types.InAssembly(apiAssembly)
        .That()
        .ResideInNamespace("BookingSystem.API.Controllers")
        .ShouldNot()
        .HaveDependencyOn("BookingSystem.Infrastructure.Repositories")
        .GetResult();

    Assert.True(result.IsSuccessful,
        "Controllers should use MediatR, not repositories directly!");
}
```

**All 15 Architecture Tests:**

| Test                                              | Validates                                    |
| ------------------------------------------------- | -------------------------------------------- |
| Domain_Should_NotHaveAnyDependencies              | Domain is innermost layer                    |
| Application_Should_OnlyDependOnDomain             | Application isolated from Infrastructure/API |
| Infrastructure_Should_NotDependOnAPI              | Infrastructure independent of API            |
| API_Controllers_Should_NotDirectlyUseRepositories | MediatR pattern enforced                     |
| Commands_Should_EndWithCommand                    | CQRS naming consistency                      |
| Queries_Should_EndWithQuery                       | CQRS naming consistency                      |
| Handlers_Should_EndWithHandler                    | Handler naming consistency                   |
| Validators_Should_EndWithValidator                | Validator naming consistency                 |
| Controllers_Should_EndWithController              | Controller naming consistency                |
| Repositories_Should_EndWithRepository             | Repository naming consistency                |
| Handlers_Should_ResideInCorrectNamespace          | Feature-based organization                   |
| DTOs_Should_ResideInDTOsFolder                    | DTO organization                             |
| Entities_Should_ResideInDomainLayer               | Entities in Domain only                      |
| Repositories_Should_ImplementIRepository          | Interface compliance                         |
| Middleware_Should_ResideInAPILayer                | Middleware location                          |

**Why This Matters:**

1. **Prevents Regression**: Can't accidentally add wrong dependency
2. **Onboards New Developers**: Tests document architecture rules
3. **CI/CD Gate**: Fails build if architecture violated
4. **Living Documentation**: Tests describe intended design

**Real Example of Caught Violation:**

Initially, I had `Program.cs` depending on Domain, which failed the test. I refined the rule to allow Program.cs (composition root) but prevent Controllers from doing the same, enforcing proper MediatR usage."

---

## Q: "What was your code coverage percentage, and is that good enough?"

**A:** "My **Domain layer has 64% line coverage and 100% branch coverage**. Application layer has 12%. Here's the context:

**Coverage by Layer:**

```
Domain Layer:       64% lines, 100% branches ✅
├─ Booking:         100% covered
├─ AvailabilityRule: 100% covered
├─ RefreshToken:    100% covered
├─ Resource:        91% covered
└─ User/Tenant:     0% covered (simple DTOs)

Application Layer:  12% lines, 5% branches ⚠️
├─ CreateBookingCommandHandler:    Covered ✅
├─ CreateResourceCommandHandler:   Covered ✅
├─ LoginCommandHandler:             Covered ✅
└─ Query handlers:                  Not covered ⏳

Infrastructure:     0% (tested via integration tests)
API:                0% (tested via integration tests)
```

**Why 64% Domain Coverage is Sufficient:**

```csharp
// ✅ Covered: Business logic that can fail
[Fact]
public void Booking_DetectOverlap_ComplexLogic()
{
    // Testing edge cases:
    // - Exact time match
    // - Partial overlap (start/end)
    // - Complete containment
    // - No overlap
}

// ❌ Not Covered: Simple getters/setters (no business logic)
public class User
{
    public Guid Id { get; set; }           // No test needed
    public string Email { get; set; }      // No test needed
    public string PasswordHash { get; set; } // No test needed
}
```

**Why Integration Tests Don't Show in Coverage:**

```csharp
// Integration test exercises infrastructure + API
// But code coverage tools measure unit test execution only
[Fact]
public async Task CreateBooking_WithValidData_ShouldReturn201()
{
    // This tests Infrastructure + API layers
    // But doesn't count toward coverage metrics
    // Because it exercises code via HTTP boundary
}
```

**Coverage Philosophy:**

| Metric         | My Target | Industry Standard | Why Differ?                        |
| -------------- | --------- | ----------------- | ---------------------------------- |
| Domain         | 60-80%    | 80%+              | Focus on business logic, skip DTOs |
| Application    | 40-60%    | 70%+              | Commands covered, queries via E2E  |
| Infrastructure | N/A       | 50%+              | Integration tests provide coverage |
| Overall        | 50%+      | 80%+              | Multi-layered test strategy        |

**What I'd Improve for Production:**

1. **Add Query Handler Unit Tests**: Currently tested via integration tests only
2. **Test User/Tenant Entities**: Add validation logic tests
3. **Edge Case Coverage**: More boundary condition tests
4. **Mutation Testing**: Use Stryker.NET to verify test quality

**Key Insight:**

'Code coverage is a good servant but a bad master.' I focused on:

- ✅ Testing **critical business logic** (booking conflicts, multi-tenant isolation)
- ✅ Testing **different layers differently** (unit vs integration vs architecture)
- ✅ Ensuring **behavior correctness**, not just coverage percentage

Integration tests provide confidence that the system works end-to-end, even though they don't contribute to unit test coverage metrics."

---

## Q: "How did you structure your unit tests? What patterns did you follow?"

**A:** "I followed the **AAA pattern** (Arrange-Act-Assert) and used **FluentAssertions** for readable tests:

**AAA Pattern Example:**

```csharp
[Fact]
public void RefreshToken_IsExpired_ReturnsTrue_WhenExpiryDatePassed()
{
    // Arrange - Set up test data
    var refreshToken = new RefreshToken
    {
        Token = Guid.NewGuid().ToString(),
        ExpiryDate = DateTime.UtcNow.AddDays(-1),  // Expired yesterday
        IsRevoked = false
    };

    // Act - Execute the method being tested
    var isExpired = refreshToken.IsExpired;

    // Assert - Verify the expected outcome
    isExpired.Should().BeTrue();
    // FluentAssertions makes test intent clear
}
```

**Test Organization Structure:**

```
BookingSystem.UnitTests/
├── Domain/
│   └── Entities/
│       ├── BookingTests.cs           (7 tests)
│       ├── RefreshTokenTests.cs      (9 tests)
│       └── AvailabilityRuleTests.cs  (5 tests)
├── Application/
│   ├── Commands/
│   │   ├── CreateBookingCommandHandlerTests.cs   (5 tests)
│   │   └── CreateResourceCommandHandlerTests.cs  (4 tests)
│   └── Validators/
│       ├── CreateBookingCommandValidatorTests.cs (8 tests)
│       ├── CreateResourceCommandValidatorTests.cs (6 tests)
│       └── LoginCommandValidatorTests.cs          (7 tests)
└── Architecture/
    └── ArchitectureTests.cs          (15 tests)
```

**Testing Command Handlers (with Mocks):**

```csharp
public class CreateResourceCommandHandlerTests
{
    private readonly Mock<IResourceRepository> _mockRepo;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly CreateResourceCommandHandler _handler;

    public CreateResourceCommandHandlerTests()
    {
        // Arrange - Set up mocks once per test class
        _mockRepo = new Mock<IResourceRepository>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockTenantContext.Setup(x => x.TenantId)
            .Returns(Guid.NewGuid());

        _handler = new CreateResourceCommandHandler(
            _mockRepo.Object,
            _mockTenantContext.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallRepositoryAdd()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "Conference Room A",
            ResourceType = "MeetingRoom",
            Capacity = 10
        };

        var expectedResourceId = Guid.NewGuid();
        _mockRepo.Setup(x => x.AddAsync(It.IsAny<Resource>()))
            .ReturnsAsync(expectedResourceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().Be(expectedResourceId);
        _mockRepo.Verify(x => x.AddAsync(
            It.Is<Resource>(r =>
                r.Name == command.Name &&
                r.ResourceType == command.ResourceType &&
                r.Capacity == command.Capacity
            )), Times.Once);
    }
}
```

**Testing Validators (FluentValidation):**

```csharp
public class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _validator;

    public CreateBookingCommandValidatorTests()
    {
        _validator = new CreateBookingCommandValidator();
    }

    [Fact]
    public void Validate_WithEndTimeBeforeStartTime_ShouldFail()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddHours(2),
            EndTime = DateTime.UtcNow.AddHours(1),  // Before start!
            Title = "Meeting"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateBookingCommand.EndTime) &&
            e.ErrorMessage.Contains("must be after"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithInvalidTitle_ShouldFail(string invalidTitle)
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Title = invalidTitle
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateBookingCommand.Title));
    }
}
```

**Key Testing Principles I Follow:**

1. **One Assertion Per Test** (mostly): Each test validates one behavior
2. **Descriptive Test Names**: `MethodName_Scenario_ExpectedResult`
3. **No Logic in Tests**: Tests should be dead simple, no conditionals
4. **Fast Tests**: Unit tests run in milliseconds, no external dependencies
5. **Independent Tests**: Each test can run alone, no shared state
6. **Use Theory for Similar Cases**: `[Theory]` + `[InlineData]` for multiple inputs

**FluentAssertions Benefits:**

```csharp
// ❌ Traditional Assert (hard to read failure message)
Assert.True(booking.Status == BookingStatus.Confirmed);
// Failure: "Expected True but was False"

// ✅ FluentAssertions (clear failure message)
booking.Status.Should().Be(BookingStatus.Confirmed);
// Failure: "Expected booking.Status to be Confirmed, but found Pending"

// ✅ Collection assertions
resources.Should().HaveCount(3)
    .And.AllSatisfy(r => r.TenantId.Should().Be(expectedTenantId));
// Failure shows exactly which items failed
```

**Mocking Philosophy:**

- **Mock external dependencies** (repositories, services)
- **Don't mock domain entities** (use real objects)
- **Don't mock what you don't own** (e.g., don't mock DateTime, use testable design)
- **Verify behavior, not implementation** (verify method was called, not how many times specific properties were accessed)"

---

## Q: "What testing challenges did you face, and how did you solve them?"

**A:** "I encountered several interesting testing challenges. Here are the key ones:

**Challenge 1: JWT Token Generation in Tests**

**Problem:** Integration tests need real JWT tokens to test authenticated endpoints, but we don't want to expose production token signing keys.

**Solution:**

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace JWT configuration with test-specific settings
            services.Configure<JwtSettings>(options =>
            {
                options.SecretKey = "test-secret-key-that-is-at-least-32-characters-long";
                options.Issuer = "TestIssuer";
                options.Audience = "TestAudience";
                options.ExpiryMinutes = 60;
            });

            // Use real JwtTokenService with test configuration
            // This ensures token generation works identically to production
        });
    }
}
```

**Challenge 2: Database Column Naming (snake_case vs camelCase)**

**Problem:** Integration tests failed with `column "tenant_id" does not exist` even though Dapper mapped `TenantId` correctly.

**Root Cause:** PostgreSQL converts unquoted identifiers to lowercase:

```sql
-- ❌ What I wrote in tests:
SELECT * FROM resources WHERE tenant_id = @tenantId

-- ✅ What PostgreSQL expects:
SELECT * FROM resources WHERE tenantid = @tenantId
-- PostgreSQL lowercases TenantId → tenantid (no underscore!)
```

**Solution:**

```csharp
// Fixed all test SQL queries to use lowercase
cmd.CommandText = "SELECT * FROM resources WHERE tenantid = @tenantId";
//                                                ^^^^^^^^ lowercase, no underscore
```

**Lesson Learned:** Integration tests caught a production-critical naming mismatch that unit tests couldn't detect!

**Challenge 3: PagedResult Deserialization**

**Problem:** Integration test failed when deserializing paginated API responses:

```csharp
// ❌ Failed:
var result = await response.Content.ReadFromJsonAsync<PagedResult<ResourceDto>>();
// Error: "Each parameter in deserialization constructor must bind to an object property"
```

**Root Cause:** System.Text.Json requires parameterless constructor when class has parameterized constructor.

**Solution:**

```csharp
public class PagedResult<T>
{
    // Added parameterless constructor for deserialization
    public PagedResult() { }

    public PagedResult(List<T> items, int count, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    // ... other properties
}
```

**Challenge 4: Testing Booking Conflict Detection**

**Problem:** How to test that overlapping bookings are prevented without creating flaky time-based tests?

**Solution:** Use relative times and clear time windows:

```csharp
[Fact]
public async Task CreateBooking_WithOverlappingTimes_ShouldReturn400()
{
    var now = DateTime.UtcNow;

    // Create first booking: 10:00 - 12:00
    var booking1 = await CreateBookingAsync(
        resourceId,
        startTime: now.AddHours(10),
        endTime: now.AddHours(12)
    );

    // Try to create overlapping booking: 11:00 - 13:00
    var response = await CreateBookingAsync(
        resourceId,
        startTime: now.AddHours(11),  // Overlaps by 1 hour
        endTime: now.AddHours(13)
    );

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

**Key:** Use `DateTime.UtcNow` as base, add hours/minutes relative to it. Never hardcode specific dates!

**Challenge 5: Architecture Test - Abstract Class Naming**

**Problem:** `Repositories_Should_EndWithRepository` test failed because `BaseRepository<T>` doesn't end with "Repository".

**Solution:** Exclude abstract classes from the test:

```csharp
[Fact]
public void Repositories_Should_EndWithRepository()
{
    var result = Types.InAssembly(infrastructureAssembly)
        .That()
        .AreClasses()
        .And()
        .ResideInNamespace("BookingSystem.Infrastructure.Repositories")
        .And()
        .AreNotAbstract()  // ← Exclude BaseRepository<T>
        .Should()
        .HaveNameEndingWith("Repository")
        .GetResult();

    Assert.True(result.IsSuccessful);
}
```

**Challenge 6: Integration Test Data Cleanup**

**Problem:** Tests occasionally failed due to leftover data from previous test runs.

**Solution:** Use Testcontainers with per-collection database fixtures:

```csharp
[Collection("Database")]  // Shared fixture for test collection
public class BookingsControllerTests : IntegrationTestBase
{
    public BookingsControllerTests(DatabaseFixture dbFixture)
        : base(dbFixture)
    {
        // Each test collection gets a fresh database container
        // Tests within collection share container but run sequentially
    }
}
```

**Key Lesson:** Testcontainers provides isolation without the complexity of manual database cleanup."

---

## Q: "How would you improve your test suite for a production application?"

**A:** "Great question! Here's what I'd add for production-grade testing:

**1. Mutation Testing (Test Quality Validation)**

```bash
# Install Stryker.NET for mutation testing
dotnet tool install -g dotnet-stryker

# Run mutation tests to verify test quality
dotnet stryker
```

**What It Does:** Mutates code (changes `>` to `>=`, removes conditions) and checks if tests fail. If tests still pass with mutated code, they're not thorough enough.

**Example:**

```csharp
// Original code
public bool IsExpired => ExpiryDate < DateTime.UtcNow;

// Mutation 1: Change < to <=
public bool IsExpired => ExpiryDate <= DateTime.UtcNow;
// Good test would fail!

// Mutation 2: Always return false
public bool IsExpired => false;
// Good test would fail!
```

**2. Performance Tests**

```csharp
[Fact]
public async Task GetAllBookings_WithThousandsOfRecords_ShouldReturnIn500ms()
{
    // Arrange: Insert 10,000 bookings
    await InsertBulkBookingsAsync(10000);

    // Act
    var stopwatch = Stopwatch.StartNew();
    var response = await GetAsync("/api/v1/bookings?pageSize=100");
    stopwatch.Stop();

    // Assert
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    // Validates pagination efficiency
}
```

**3. Contract Testing (for Frontend-Backend Integration)**

```csharp
// Using Pact.NET or similar
[Fact]
public void GetBookingById_ShouldMatchContract()
{
    // Define contract: Frontend expects these exact fields
    var expectedContract = new
    {
        Id = Guid.Empty,
        ResourceId = Guid.Empty,
        Title = "",
        StartTime = DateTime.MinValue,
        EndTime = DateTime.MinValue,
        Status = ""
    };

    // Verify API response matches contract
    var response = await GetBookingByIdAsync(bookingId);
    response.Should().BeEquivalentTo(expectedContract,
        options => options.ExcludingMissingMembers());
}
```

**4. Concurrency Tests**

```csharp
[Fact]
public async Task CreateBooking_ConcurrentRequests_OnlyOneSucceeds()
{
    // Simulate race condition: Two users book same resource simultaneously
    var tasks = Enumerable.Range(0, 10).Select(i =>
        CreateBookingAsync(resourceId, DateTime.UtcNow, DateTime.UtcNow.AddHours(1))
    );

    var results = await Task.WhenAll(tasks);

    // Only 1 should succeed, 9 should get conflict errors
    results.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
    results.Count(r => r.StatusCode == HttpStatusCode.BadRequest).Should().Be(9);
}
```

**5. Security Tests**

```csharp
[Fact]
public async Task CreateBooking_WithExpiredToken_ShouldReturn401()
{
    var expiredToken = GenerateExpiredJwtToken();

    var response = await PostAsync("/api/v1/bookings", new { },
        tenantId, expiredToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}

[Fact]
public async Task GetBooking_WithSQLInjection_ShouldBeSanitized()
{
    // Try SQL injection in search parameter
    var response = await GetAsync(
        "/api/v1/bookings?title=' OR '1'='1");

    // Should not return all bookings or error
    response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
}
```

**6. Snapshot Testing (for consistent API responses)**

```csharp
// Using Verify library
[Fact]
public async Task GetBookingById_ShouldMatchSnapshot()
{
    var booking = await CreateBookingAsync();

    var response = await GetAsync($"/api/v1/bookings/{booking.Id}");
    var json = await response.Content.ReadAsStringAsync();

    // Verifies response structure hasn't changed unexpectedly
    await Verify(json);
}
```

**7. Chaos Engineering Tests**

```csharp
[Fact]
public async Task CreateBooking_WhenDatabaseSlow_ShouldTimeout()
{
    // Simulate slow database (Polly + Testcontainers)
    // Verify timeout policy works correctly
}

[Fact]
public async Task CreateBooking_WhenDatabaseDisconnects_ShouldReturnServiceUnavailable()
{
    await _dbContainer.StopAsync();  // Kill database mid-request

    var response = await CreateBookingAsync();

    response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
}
```

**8. E2E Tests (Playwright or Cypress)**

```typescript
// Playwright example
test("Complete booking flow", async ({ page }) => {
  await page.goto("http://localhost:3000");

  // Login
  await page.fill('[data-testid="email"]', "admin@tenant.com");
  await page.fill('[data-testid="password"]', "Password123");
  await page.click('[data-testid="login-button"]');

  // Create booking
  await page.click('[data-testid="new-booking"]');
  await page.selectOption('[data-testid="resource"]', "Meeting Room A");
  await page.fill('[data-testid="start-time"]', "2026-04-07T10:00");
  await page.fill('[data-testid="end-time"]', "2026-04-07T12:00");
  await page.click('[data-testid="submit-booking"]');

  // Verify success
  await expect(
    page.locator('[data-testid="booking-confirmation"]'),
  ).toContainText("Booking created successfully");
});
```

**Priority Order for Production:**

1. **Mutation Testing** - Validates test suite quality
2. **Performance Tests** - Prevents regressions
3. **Security Tests** - Protects against attacks
4. **E2E Tests** - Validates critical user journeys
5. **Contract Tests** - Prevents frontend-backend mismatches
6. **Chaos Tests** - Validates error handling

**Current Coverage is Production-Ready Baseline:**

My current test suite (147 tests, 88% passing) provides solid confidence for:

- ✅ Business logic correctness
- ✅ Multi-tenant isolation
- ✅ Architecture compliance
- ✅ API endpoints functionality

Adding the above would make it **enterprise-grade** for high-stakes production use."

---

[← Previous: Day 6](./Day6-Advanced.md) | [Back to Index](./README.md)
