using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BookingSystem.Application.Common.Interfaces;

namespace BookingSystem.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Configures the test server with test-specific settings.
/// </summary>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration for testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "TestSecretKeyForIntegrationTestsAtLeast32Characters",
                ["Jwt:Issuer"] = "BookingSystemTestIssuer",
                ["Jwt:Audience"] = "BookingSystemTestAudience",
                ["Jwt:ExpirationMinutes"] = "60",
                // ConnectionString will be set per test (using Testcontainers)
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Note: Database connection will be configured per test using Testcontainers
            // Tests should override IDbConnectionFactory with test container connection string
            
            // Rate limiting will be handled by configuration override (set limits very high for tests)
        });

        builder.UseEnvironment("Test");
    }
}
