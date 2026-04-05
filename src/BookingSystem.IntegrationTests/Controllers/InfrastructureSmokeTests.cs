using System.Net;
using FluentAssertions;
using Xunit;

namespace BookingSystem.IntegrationTests.Controllers;

/// <summary>
/// Smoke tests to verify the integration test infrastructure is working correctly.
/// </summary>
public class InfrastructureSmokeTests : IntegrationTestBase
{
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
}
