using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Infrastructure.Data;
using BookingSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace BookingSystem.IntegrationTests;

/// <summary>
/// Base class for integration tests with database support.
/// Provides common setup, HTTP client, and helper methods.
/// Implements IAsyncLifetime for proper async initialization and cleanup.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime, IDisposable
{
    protected readonly DatabaseFixture DatabaseFixture;
    protected TestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected IServiceScope Scope { get; private set; } = null!;

    // Helper properties for commonly used services
    protected IDbConnectionFactory DbConnectionFactory => 
        Scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

    protected IntegrationTestBase(DatabaseFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture;
    }

    /// <summary>
    /// Called before each test. Override to add test-specific setup.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        Factory = new TestWebApplicationFactory(DatabaseFixture.ConnectionString);
        
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        Scope = Factory.Services.CreateScope();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Called after each test. Override to add test-specific cleanup.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        Scope?.Dispose();
        Client?.Dispose();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        Factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region HTTP Helper Methods

    /// <summary>
    /// Sends a GET request to the specified URI.
    /// </summary>
    protected Task<HttpResponseMessage> GetAsync(string uri, string? tenantId = null, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        AddAuthHeaders(request, tenantId, token);
        return Client.SendAsync(request);
    }

    /// <summary>
    /// Sends a POST request with JSON body.
    /// </summary>
    protected Task<HttpResponseMessage> PostAsync<T>(string uri, T body, string? tenantId = null, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        AddAuthHeaders(request, tenantId, token);
        return Client.SendAsync(request);
    }

    /// <summary>
    /// Sends a PUT request with JSON body.
    /// </summary>
    protected Task<HttpResponseMessage> PutAsync<T>(string uri, T body, string? tenantId = null, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        AddAuthHeaders(request, tenantId, token);
        return Client.SendAsync(request);
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    protected Task<HttpResponseMessage> DeleteAsync(string uri, string? tenantId = null, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        AddAuthHeaders(request, tenantId, token);
        return Client.SendAsync(request);
    }

    /// <summary>
    /// Adds authentication headers to the request.
    /// </summary>
    private void AddAuthHeaders(HttpRequestMessage request, string? tenantId, string? token)
    {
        if (!string.IsNullOrEmpty(tenantId))
        {
            request.Headers.Add("X-Tenant-Id", tenantId);
        }

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    #endregion

    #region Authentication Helper Methods

    /// <summary>
    /// Registers a new tenant and returns the response with access token.
    /// </summary>
    protected async Task<(string TenantId, string Token, HttpResponseMessage Response)> RegisterTenantAsync(
        string tenantName = "Test Tenant",
        string email = "admin@test.com",
        string password = "Test1234",
        string firstName = "Test",
        string lastName = "Admin",
        string plan = "Pro")
    {
        var request = new
        {
            TenantName = tenantName,
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            Plan = plan
        };

        var response = await Client.PostAsJsonAsync("/api/v1/auth/register-tenant", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<RegisterTenantResponseDto>();
            return (result!.TenantId, result.AccessToken, response);
        }

        return (string.Empty, string.Empty, response);
    }

    /// <summary>
    /// Logs in and returns the access token.
    /// </summary>
    protected async Task<(string Token, HttpResponseMessage Response)> LoginAsync(
        string email,
        string password)
    {
        var request = new
        {
            Email = email,
            Password = password
        };

        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            return (result!.AccessToken, response);
        }

        return (string.Empty, response);
    }

    #endregion

    #region DTOs (for deserialization)

    private record RegisterTenantResponseDto(
        string TenantId,
        string UserId,
        string AccessToken,
        string RefreshToken);

    private record LoginResponseDto(
        string UserId,
        string Email,
        string AccessToken,
        string RefreshToken);

    #endregion
}
