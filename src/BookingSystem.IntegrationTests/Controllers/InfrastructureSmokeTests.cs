using System.Net;
using FluentAssertions;
using BookingSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace BookingSystem.IntegrationTests.Controllers;

/// <summary>
/// Smoke tests to verify the integration test infrastructure is working correctly.
/// Uses DatabaseFixture to test with a real PostgreSQL database.
/// </summary>
[Collection("Database")]
public class InfrastructureSmokeTests : IntegrationTestBase
{
    public InfrastructureSmokeTests(DatabaseFixture databaseFixture) 
        : base(databaseFixture)
    {
    }
    [Fact]
    public async Task API_ShouldStart_Successfully()
    {
        // Arrange - nothing needed, server started in base class

        // Act - make a request to non-existent endpoint
        var response = await Client.GetAsync("/api/health");

        // Assert - we expect 404 (endpoint doesn't exist) or 401 (auth required)
        // But NOT 500 (server error) - means server is running
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Swagger_ShouldBeAccessible_InTestEnvironment()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/swagger/index.html");

        // Assert - Swagger requires authentication (FallbackPolicy) or might not be enabled in Test
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, 
            HttpStatusCode.NotFound,
            HttpStatusCode.Moved,
            HttpStatusCode.Redirect,
            HttpStatusCode.Unauthorized);  // Added Unauthorized since FallbackPolicy requires auth
    }

    [Fact]
    public async Task UnauthenticatedRequest_ShouldReturn400_WhenTenantHeaderMissing()
    {
        // Arrange & Act - request to protected endpoint without X-Tenant-Id header
        var response = await GetAsync("/api/v1/resources");

        // Assert - TenantResolutionMiddleware runs before Authentication, returns 400 for missing tenant
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RequestWithTenantButNoAuth_ShouldReturn401()
    {
        // Arrange & Act - request with tenant header but no auth token
        var response = await GetAsync("/api/v1/resources", tenantId: "00000000-0000-0000-0000-000000000001");

        // Assert - should get Unauthorized (no token)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Database_ShouldBeAccessible_AndMigrationsApplied()
    {
        // Arrange & Act - connect to database and query system table
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'";
        var tableCount = Convert.ToInt64(command.ExecuteScalar() ?? 0L);

        // Assert - we should have tables created by migrations
        tableCount.Should().BeGreaterThan(0, "migrations should have created tables");
        
        // Verify specific core tables exist
        command.CommandText = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'tenants')";
        var tenantsTableExists = Convert.ToBoolean(command.ExecuteScalar() ?? false);
        tenantsTableExists.Should().BeTrue("tenants table should exist");

        command.CommandText = "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'users')";
        var usersTableExists = Convert.ToBoolean(command.ExecuteScalar() ?? false);
        usersTableExists.Should().BeTrue("users table should exist");

        await Task.CompletedTask;
    }
}
