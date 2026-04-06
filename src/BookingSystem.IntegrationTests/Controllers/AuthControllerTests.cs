using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using BookingSystem.Application.Features.Authentication.DTOs;
using BookingSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BookingSystem.IntegrationTests.Controllers;

[Collection("Database")]
public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }

    [Fact]
    public async Task RegisterTenant_WithValidData_ShouldReturn201AndAuthResult()
    {
        // Arrange
        var request = new RegisterTenantRequest
        {
            TenantName = "Acme Corporation",
            Email = "admin@acme.com",
            Password = "SecurePass123!",
            FirstName = "John",
            LastName = "Admin",
            Plan = "Premium"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register-tenant", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<RegisterTenantResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("Tenant registered successfully");
        result.AuthResult.Should().NotBeNull();
        result.AuthResult.Token.Should().NotBeNullOrEmpty();
        result.AuthResult.RefreshToken.Should().NotBeNullOrEmpty();
        result.AuthResult.Email.Should().Be(request.Email);
        result.AuthResult.FirstName.Should().Be(request.FirstName);
        result.AuthResult.LastName.Should().Be(request.LastName);
        result.AuthResult.TenantName.Should().Be(request.TenantName);
        result.AuthResult.Roles.Should().Contain("TenantAdmin");
        result.AuthResult.TenantId.Should().NotBeEmpty();
        result.AuthResult.UserId.Should().NotBeEmpty();

        // Verify JWT token has correct claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.AuthResult.Token);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == request.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "tenantId");
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);

        // Verify data persisted in database
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();
        
        var tenantCmd = connection.CreateCommand();
        tenantCmd.CommandText = "SELECT COUNT(*) FROM tenants WHERE email = @email";
        var emailParam = tenantCmd.CreateParameter();
        emailParam.ParameterName = "@email";
        emailParam.Value = request.Email;
        tenantCmd.Parameters.Add(emailParam);
        var tenantCount = Convert.ToInt64(tenantCmd.ExecuteScalar() ?? 0L);
        tenantCount.Should().Be(1, "tenant should be created in database");

        var userCmd = connection.CreateCommand();
        userCmd.CommandText = "SELECT COUNT(*) FROM users WHERE email = @email";
        var userEmailParam = userCmd.CreateParameter();
        userEmailParam.ParameterName = "@email";
        userEmailParam.Value = request.Email;
        userCmd.Parameters.Add(userEmailParam);
        var userCount = Convert.ToInt64(userCmd.ExecuteScalar() ?? 0L);
        userCount.Should().Be(1, "admin user should be created in database");
    }

    [Fact]
    public async Task RegisterTenant_WithDuplicateEmail_ShouldReturn400()
    {
        // Arrange - Create first tenant
        var firstRequest = new RegisterTenantRequest
        {
            TenantName = "First Company",
            Email = "duplicate@test.com",
            Password = "SecurePass123!",
            FirstName = "First",
            LastName = "User",
            Plan = "Free"
        };
        await Client.PostAsJsonAsync("/api/v1/auth/register-tenant", firstRequest);

        // Act - Try to create second tenant with same email
        var duplicateRequest = new RegisterTenantRequest
        {
            TenantName = "Second Company",
            Email = "duplicate@test.com", // Same email
            Password = "AnotherPass456!",
            FirstName = "Second",
            LastName = "User",
            Plan = "Premium"
        };
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register-tenant", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterTenant_WithInvalidData_ShouldReturn400()
    {
        // Arrange
        var request = new RegisterTenantRequest
        {
            TenantName = "", // Invalid: empty tenant name
            Email = "invalid-email", // Invalid: not a valid email format
            Password = "123", // Invalid: too short
            FirstName = "",
            LastName = "",
            Plan = "Free"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register-tenant", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterUser_WithValidDataAndTenantHeader_ShouldReturn201()
    {
        // Arrange - First create a tenant
        var tenantResponse = await RegisterTenantAsync("Test Tenant", "tenant@test.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId;

        var request = new RegisterUserRequest
        {
            Email = "newuser@test.com",
            Password = "UserPass123!",
            FirstName = "New",
            LastName = "User",
            Roles = new List<string> { "User" }
        };

        // Act
        var response = await PostAsync($"/api/v1/auth/register-user", request, tenantId.ToString());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("User registered successfully");
        result.AuthResult.Should().NotBeNull();
        result.AuthResult.UserId.Should().NotBeEmpty();
        result.AuthResult.Email.Should().Be(request.Email);
        result.AuthResult.TenantId.Should().Be(tenantId);

        // Verify user persisted in database with correct tenant
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT tenant_id FROM users WHERE email = @email";
        var emailParam = cmd.CreateParameter();
        emailParam.ParameterName = "@email";
        emailParam.Value = request.Email;
        cmd.Parameters.Add(emailParam);
        var resultTenantId = Guid.Parse(cmd.ExecuteScalar()?.ToString() ?? Guid.Empty.ToString());
        resultTenantId.Should().Be(tenantId, "user should belong to correct tenant");
    }

    [Fact]
    public async Task RegisterUser_WithoutTenantHeader_ShouldReturn400()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            Email = "user@test.com",
            Password = "UserPass123!",
            FirstName = "Test",
            LastName = "User",
            Roles = new List<string> { "User" }
        };

        // Act - Don't include X-Tenant-Id header
        var response = await Client.PostAsJsonAsync("/api/v1/auth/register-user", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200AndToken()
    {
        // Arrange - Register a tenant first
        var email = "logintest@test.com";
        var password = "TestPass123!";
        await RegisterTenantAsync("Login Test Tenant", email, password);

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Be("Login successful");
        result.AuthResult.Should().NotBeNull();
        result.AuthResult.Token.Should().NotBeNullOrEmpty();
        result.AuthResult.RefreshToken.Should().NotBeNullOrEmpty();
        result.AuthResult.Email.Should().Be(email);
        result.AuthResult.TenantName.Should().Be("Login Test Tenant");
        result.AuthResult.Roles.Should().Contain("TenantAdmin");

        // Verify token is valid JWT
        var handler = new JwtSecurityTokenHandler();
        var canRead = handler.CanReadToken(result.AuthResult.Token);
        canRead.Should().BeTrue("token should be valid JWT");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401()
    {
        // Arrange - Register a tenant first
        var email = "passwordtest@test.com";
        await RegisterTenantAsync("Password Test Tenant", email, "CorrectPass123!");

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "WrongPassword!" // Incorrect password
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldReturn401()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@test.com",
            Password = "SomePassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturn200AndNewTokens()
    {
        // Arrange - Register and login to get refresh token
        var email = "refreshtest@test.com";
        var password = "TestPass123!";
        var loginResponse = await RegisterTenantAsync("Refresh Test Tenant", email, password);
        var originalRefreshToken = loginResponse.AuthResult.RefreshToken;
        var originalAccessToken = loginResponse.AuthResult.Token;

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = originalRefreshToken
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        result.Should().NotBeNull();
        result!.AuthResult.Should().NotBeNull();
        result.AuthResult!.Token.Should().NotBeNullOrEmpty();
        result.AuthResult.RefreshToken.Should().NotBeNullOrEmpty();
        result.AuthResult.Token.Should().NotBe(originalAccessToken, "new access token should be different");
        result.AuthResult.RefreshToken.Should().NotBe(originalRefreshToken, "new refresh token should be different (token rotation)");

        // Verify new tokens are valid
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(result.AuthResult.Token).Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ShouldReturn401()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token-12345"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ShouldReturn401()
    {
        // Arrange - This test verifies behavior with an already-used token
        var email = "expiredrefresh@test.com";
        var password = "TestPass123!";
        var loginResponse = await RegisterTenantAsync("Expired Refresh Tenant", email, password);
        var refreshToken = loginResponse.AuthResult.RefreshToken;

        // First refresh - this should succeed and invalidate the original token
        var firstRefreshRequest = new RefreshTokenRequest { RefreshToken = refreshToken };
        await Client.PostAsJsonAsync("/api/v1/auth/refresh", firstRefreshRequest);

        // Act - Try to use the same refresh token again (should fail - token rotation)
        var secondRefreshRequest = new RefreshTokenRequest { RefreshToken = refreshToken };
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", secondRefreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MultiTenant_UsersWithSameEmail_ShouldBeIsolatedByTenant()
    {
        // Arrange - Create two tenants with users having the same email (different tenants)
        var sharedEmail = "user@company.com";
        var password1 = "TenantOnePass123!";
        var password2 = "TenantTwoPass123!";

        // Tenant 1
        var tenant1Response = await RegisterTenantAsync("Tenant One", "admin1@tenant1.com", "AdminPass1!");
        var tenant1Id = tenant1Response.AuthResult.TenantId;
        await PostAsync("/api/v1/auth/register-user", new RegisterUserRequest
        {
            Email = sharedEmail,
            Password = password1,
            FirstName = "User",
            LastName = "One",
            Roles = new List<string> { "User" }
        }, tenant1Id.ToString());

        // Tenant 2
        var tenant2Response = await RegisterTenantAsync("Tenant Two", "admin2@tenant2.com", "AdminPass2!");
        var tenant2Id = tenant2Response.AuthResult.TenantId;
        await PostAsync("/api/v1/auth/register-user", new RegisterUserRequest
        {
            Email = sharedEmail,
            Password = password2,
            FirstName = "User",
            LastName = "Two",
            Roles = new List<string> { "User" }
        }, tenant2Id.ToString());

        // Act - Login as Tenant 1 user
        var login1 = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = sharedEmail,
            Password = password1
        });

        // Act - Login as Tenant 2 user
        var login2 = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = sharedEmail,
            Password = password2
        });

        // Assert - Both logins should succeed but return different tenant contexts
        login1.StatusCode.Should().Be(HttpStatusCode.OK);
        login2.StatusCode.Should().Be(HttpStatusCode.OK);

        var result1 = await login1.Content.ReadFromJsonAsync<LoginResponse>();
        var result2 = await login2.Content.ReadFromJsonAsync<LoginResponse>();

        result1!.AuthResult.TenantId.Should().Be(tenant1Id);
        result1.AuthResult.TenantName.Should().Be("Tenant One");
        result1.AuthResult.FirstName.Should().Be("User");
        result1.AuthResult.LastName.Should().Be("One");

        result2!.AuthResult.TenantId.Should().Be(tenant2Id);
        result2.AuthResult.TenantName.Should().Be("Tenant Two");
        result2.AuthResult.FirstName.Should().Be("User");
        result2.AuthResult.LastName.Should().Be("Two");

        // Verify database has 2 users with same email but different tenants
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM users WHERE email = @email";
        var emailParam = cmd.CreateParameter();
        emailParam.ParameterName = "@email";
        emailParam.Value = sharedEmail;
        cmd.Parameters.Add(emailParam);
        var userCount = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        userCount.Should().Be(2, "two users with same email should exist in different tenants");
    }

    [Fact]
    public async Task Login_WithWrongTenantPassword_ShouldNotAuthorizeInDifferentTenant()
    {
        // Arrange - Create two tenants with users having the same email
        var sharedEmail = "crosstenanttest@company.com";
        var tenant1Password = "Tenant1Pass123!";
        var tenant2Password = "Tenant2Pass123!";

        // Tenant 1
        var tenant1Response = await RegisterTenantAsync("Cross Tenant One", "admin1@crosstenant1.com", "AdminPass1!");
        await PostAsync("/api/v1/auth/register-user", new RegisterUserRequest
        {
            Email = sharedEmail,
            Password = tenant1Password,
            FirstName = "CrossUser",
            LastName = "One",
            Roles = new List<string> { "User" }
        }, tenant1Response.AuthResult.TenantId.ToString());

        // Tenant 2
        var tenant2Response = await RegisterTenantAsync("Cross Tenant Two", "admin2@crosstenant2.com", "AdminPass2!");
        await PostAsync("/api/v1/auth/register-user", new RegisterUserRequest
        {
            Email = sharedEmail,
            Password = tenant2Password,
            FirstName = "CrossUser",
            LastName = "Two",
            Roles = new List<string> { "User" }
        }, tenant2Response.AuthResult.TenantId.ToString());

        // Act - Try to login with Tenant 1 password while being Tenant 2 user (should fail)
        var loginAttempt = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = sharedEmail,
            Password = tenant1Password
        });

        // Assert - Should login as Tenant 1 user (not Tenant 2)
        loginAttempt.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await loginAttempt.Content.ReadFromJsonAsync<LoginResponse>();
        result!.AuthResult.TenantName.Should().Be("Cross Tenant One", "should authenticate to the correct tenant");
    }
}
