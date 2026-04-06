using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Infrastructure.Data;

namespace BookingSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Database fixture that manages PostgreSQL Testcontainer lifecycle.
/// Implements IAsyncLifetime to properly initialize and dispose the container.
/// Shared across all tests in a collection to avoid spinning up multiple containers.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Called once before all tests in the collection.
    /// Starts the PostgreSQL container and runs migrations.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create PostgreSQL container using builder
        // Note: Suppressing obsolete warning - builder pattern still recommended for configuration
        #pragma warning disable CS0618
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("BookingSystemTestDB")
            .WithUsername("testuser")
            .WithPassword("testpass123")
            .WithCleanUp(true)
            .Build();
        #pragma warning restore CS0618

        // Start the container
        await _postgresContainer.StartAsync();

        // Get connection string
        ConnectionString = _postgresContainer.GetConnectionString();

        // Run database migrations
        DatabaseMigration.RunMigrations(ConnectionString);
    }

    /// <summary>
    /// Called once after all tests in the collection.
    /// Stops and disposes the PostgreSQL container.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for database tests.
/// All test classes decorated with [Collection("Database")] will share the same DatabaseFixture instance.
/// This prevents spinning up a new container for each test class, improving test performance.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
