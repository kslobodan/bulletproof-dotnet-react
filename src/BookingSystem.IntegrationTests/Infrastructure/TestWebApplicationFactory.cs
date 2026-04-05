using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that uses a real PostgreSQL database from Testcontainers.
/// Overrides the IDbConnectionFactory to use the test database connection string.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TestWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration for testing
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Jwt:SecretKey"] = "TestSecretKeyForIntegrationTestsAtLeast32Characters",
                ["Jwt:Issuer"] = "BookingSystemTestIssuer",
                ["Jwt:Audience"] = "BookingSystemTestAudience",
                ["Jwt:ExpirationMinutes"] = "60",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace IDbConnectionFactory with test database connection
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(sp => 
                new TestDbConnectionFactory(_connectionString));
        });

        builder.UseEnvironment("Test");
    }
}

/// <summary>
/// Test-specific DbConnectionFactory that uses a direct connection string.
/// </summary>
internal class TestDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public TestDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public System.Data.IDbConnection CreateConnection()
    {
        return new Npgsql.NpgsqlConnection(_connectionString);
    }
}
