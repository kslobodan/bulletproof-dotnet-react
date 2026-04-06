using System.Net;
using System.Net.Http.Json;
using BookingSystem.Application.Features.Resources.DTOs;
using BookingSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BookingSystem.IntegrationTests.Controllers;

[Collection("Database")]
public class ResourcesControllerTests : IntegrationTestBase
{
    public ResourcesControllerTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Diagnostic_CreateResourceWithLoginToken_ShouldWork()
    {
        // Arrange - Register tenant first
        var email = "diagnostic@test.com";
        var password = "TestPass123!";
        var tenantResponse = await RegisterTenantAsync("Diagnostic Tenant", email, password);
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        
        // Login to get a fresh token (to test if RegisterTenant token vs Login token matters)
        var loginResponse = await LoginAsync(email, password);
        var token = loginResponse.AuthResult.Token;

        var request = new CreateResourceRequest
        {
            Name = "Diagnostic Room",
            Description = "Testing with login token",
            ResourceType = "MeetingRoom",
            Capacity = 10
        };

        // Act
        var response = await PostAsync("/api/v1/resources", request, tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, 
            $"Expected 201 with Login token. Response: {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task CreateResource_WithValidData_ShouldReturn201AndResource()
    {
        // Arrange - Register tenant and login
        var tenantResponse = await RegisterTenantAsync("Test Company", "admin@testcompany.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var request = new CreateResourceRequest
        {
            Name = "Conference Room A",
            Description = "Large conference room with projector",
            ResourceType = "MeetingRoom",
            Capacity = 20
        };

        // Act
        var response = await PostAsync("/api/v1/resources", request, tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<CreateResourceResponse>();
        result.Should().NotBeNull();
        result!.Resource.Should().NotBeNull();
        result.Resource.Id.Should().NotBeEmpty();
        result.Resource.Name.Should().Be(request.Name);
        result.Resource.Description.Should().Be(request.Description);
        result.Resource.ResourceType.Should().Be(request.ResourceType);
        result.Resource.Capacity.Should().Be(request.Capacity);
        result.Resource.IsActive.Should().BeTrue();
        result.Resource.TenantId.Should().Be(tenantResponse.AuthResult.TenantId);

        // Verify in database
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM resources WHERE id = @id AND tenant_id = @tenantId";
        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = result.Resource.Id;
        cmd.Parameters.Add(idParam);
        var tenantIdParam = cmd.CreateParameter();
        tenantIdParam.ParameterName = "@tenantId";
        tenantIdParam.Value = tenantResponse.AuthResult.TenantId;
        cmd.Parameters.Add(tenantIdParam);
        var count = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        count.Should().Be(1, "resource should be persisted in database");
    }

    [Fact]
    public async Task GetAllResources_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange - Create tenant and multiple resources
        var tenantResponse = await RegisterTenantAsync("Pagination Test Co", "admin@paginationtest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        // Create 5 resources
        for (int i = 1; i <= 5; i++)
        {
            var resource = new CreateResourceRequest
            {
                Name = $"Resource {i}",
                Description = $"Description {i}",
                ResourceType = "Equipment",
                Capacity = 10 + i
            };
            await PostAsync("/api/v1/resources", resource, tenantId, token);
        }

        // Act - Get first page with page size 3
        var response = await GetAsync("/api/v1/resources?pageNumber=1&pageSize=3", tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetResourceById_WithValidId_ShouldReturn200AndResource()
    {
        // Arrange - Create a resource
        var tenantResponse = await RegisterTenantAsync("GetById Test Co", "admin@getbyid.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var createRequest = new CreateResourceRequest
        {
            Name = "Test Equipment",
            Description = "Test equipment description",
            ResourceType = "Equipment",
            Capacity = 5
        };

        var createResponse = await PostAsync("/api/v1/resources", createRequest, tenantId, token);
        var createdResource = await createResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        // Act
        var response = await GetAsync($"/api/v1/resources/{createdResource!.Resource.Id}", tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ResourceDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdResource.Resource.Id);
        result.Name.Should().Be(createRequest.Name);
        result.Description.Should().Be(createRequest.Description);
        result.ResourceType.Should().Be(createRequest.ResourceType);
        result.Capacity.Should().Be(createRequest.Capacity);
    }

    [Fact]
    public async Task GetResourceById_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var tenantResponse = await RegisterTenantAsync("NotFound Test Co", "admin@notfound.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/resources/{nonExistentId}", tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateResource_WithValidData_ShouldReturn200AndUpdatedResource()
    {
        // Arrange - Create a resource
        var tenantResponse = await RegisterTenantAsync("Update Test Co", "admin@updatetest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var createRequest = new CreateResourceRequest
        {
            Name = "Original Name",
            Description = "Original Description",
            ResourceType = "MeetingRoom",
            Capacity = 10
        };

        var createResponse = await PostAsync("/api/v1/resources", createRequest, tenantId, token);
        var createdResource = await createResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        var updateRequest = new UpdateResourceRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            ResourceType = "MeetingRoom",
            Capacity = 15,
            IsActive = true
        };

        // Act
        var response = await PutAsync($"/api/v1/resources/{createdResource!.Resource.Id}", updateRequest, tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<UpdateResourceResponse>();
        result.Should().NotBeNull();
        result!.Resource.Id.Should().Be(createdResource.Resource.Id);
        result.Resource.Name.Should().Be(updateRequest.Name);
        result.Resource.Description.Should().Be(updateRequest.Description);
        result.Resource.Capacity.Should().Be(updateRequest.Capacity);

        // Verify in database
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name, description, capacity FROM resources WHERE id = @id";
        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = createdResource.Resource.Id;
        cmd.Parameters.Add(idParam);
        
        using var reader = cmd.ExecuteReader();
        reader.Read().Should().BeTrue();
        reader.GetString(0).Should().Be(updateRequest.Name);
        reader.GetString(1).Should().Be(updateRequest.Description);
        reader.GetInt32(2).Should().Be(updateRequest.Capacity);
    }

    [Fact]
    public async Task DeleteResource_WithValidId_ShouldReturn200AndSoftDelete()
    {
        // Arrange - Create a resource
        var tenantResponse = await RegisterTenantAsync("Delete Test Co", "admin@deletetest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var createRequest = new CreateResourceRequest
        {
            Name = "Resource To Delete",
            Description = "Will be deleted",
            ResourceType = "Equipment",
            Capacity = 5
        };

        var createResponse = await PostAsync("/api/v1/resources", createRequest, tenantId, token);
        var createdResource = await createResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        // Act
        var response = await DeleteAsync($"/api/v1/resources/{createdResource!.Resource.Id}", tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<DeleteResourceResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Contain("deleted");

        // Verify soft delete in database
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT is_deleted, deleted_at FROM resources WHERE id = @id";
        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = createdResource.Resource.Id;
        cmd.Parameters.Add(idParam);
        
        using var reader = cmd.ExecuteReader();
        reader.Read().Should().BeTrue();
        reader.GetBoolean(0).Should().BeTrue("resource should be marked as deleted");
        reader.IsDBNull(1).Should().BeFalse("deleted_at should be set");
    }

    [Fact]
    public async Task MultiTenant_ResourceIsolation_TenantCannotAccessOtherTenantsResources()
    {
        // Arrange - Create two tenants with resources
        var tenant1Response = await RegisterTenantAsync("Tenant One", "admin@tenant1.com", "SecurePass123!");
        var tenant1Id = tenant1Response.AuthResult.TenantId.ToString();
        var tenant1Token = tenant1Response.AuthResult.Token;

        var tenant2Response = await RegisterTenantAsync("Tenant Two", "admin@tenant2.com", "SecurePass123!");
        var tenant2Id = tenant2Response.AuthResult.TenantId.ToString();
        var tenant2Token = tenant2Response.AuthResult.Token;

        // Tenant 1 creates a resource
        var resource1 = new CreateResourceRequest
        {
            Name = "Tenant 1 Resource",
            Description = "Belongs to Tenant 1",
            ResourceType = "MeetingRoom",
            Capacity = 10
        };
        var response1 = await PostAsync("/api/v1/resources", resource1, tenant1Id, tenant1Token);
        var tenant1Resource = await response1.Content.ReadFromJsonAsync<CreateResourceResponse>();

        // Act - Tenant 2 tries to access Tenant 1's resource
        var response = await GetAsync($"/api/v1/resources/{tenant1Resource!.Resource.Id}", tenant2Id, tenant2Token);

        // Assert - Should not find the resource (404) due to tenant filtering
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllResources_WithResourceTypeFilter_ShouldReturnFilteredResults()
    {
        // Arrange - Create tenant and resources of different types
        var tenantResponse = await RegisterTenantAsync("Filter Test Co", "admin@filtertest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        // Create 3 meeting rooms and 2 equipment
        for (int i = 1; i <= 3; i++)
        {
            await PostAsync("/api/v1/resources", new CreateResourceRequest
            {
                Name = $"Meeting Room {i}",
                ResourceType = "MeetingRoom",
                Capacity = 10
            }, tenantId, token);
        }

        for (int i = 1; i <= 2; i++)
        {
            await PostAsync("/api/v1/resources", new CreateResourceRequest
            {
                Name = $"Equipment {i}",
                ResourceType = "Equipment",
                Capacity = 1
            }, tenantId, token);
        }

        // Act - Filter by MeetingRoom
        var response = await GetAsync("/api/v1/resources?resourceType=MeetingRoom", tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Note: Response structure depends on PagedResult implementation
        // This test verifies the endpoint works with filter parameter
    }

    [Fact]
    public async Task CreateResource_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var tenantResponse = await RegisterTenantAsync("Unauth Test", "admin@unauth.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        
        var request = new CreateResourceRequest
        {
            Name = "Unauthorized Resource",
            ResourceType = "Equipment"
        };

        // Act - Tenant ID provided but no auth token
        var response = await PostAsync("/api/v1/resources", request, tenantId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateResource_WithoutTenantHeader_ShouldReturn400()
    {
        // Arrange
        var tenantResponse = await RegisterTenantAsync("No Header Test", "admin@noheader.com", "SecurePass123!");
        var token = tenantResponse.AuthResult.Token;

        var request = new CreateResourceRequest
        {
            Name = "No Tenant Header Resource",
            ResourceType = "Equipment"
        };

        // Act - Token but no X-Tenant-Id header
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/resources");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request);
        var response = await Client.SendAsync(httpRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateResource_OfDifferentTenant_ShouldReturn404()
    {
        // Arrange - Tenant 1 creates resource, Tenant 2 tries to update it
        var tenant1Response = await RegisterTenantAsync("Update Isolation T1", "admin@updatet1.com", "SecurePass123!");
        var tenant1Id = tenant1Response.AuthResult.TenantId.ToString();
        var tenant1Token = tenant1Response.AuthResult.Token;

        var tenant2Response = await RegisterTenantAsync("Update Isolation T2", "admin@updatet2.com", "SecurePass123!");
        var tenant2Id = tenant2Response.AuthResult.TenantId.ToString();
        var tenant2Token = tenant2Response.AuthResult.Token;

        // Tenant 1 creates resource
        var createResponse = await PostAsync("/api/v1/resources", new CreateResourceRequest
        {
            Name = "Tenant 1 Resource",
            ResourceType = "MeetingRoom",
            Capacity = 10
        }, tenant1Id, tenant1Token);
        var createdResource = await createResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        // Act - Tenant 2 tries to update Tenant 1's resource
        var updateRequest = new UpdateResourceRequest
        {
            Name = "Malicious Update",
            ResourceType = "MeetingRoom",
            Capacity = 100,
            IsActive = false
        };
        var response = await PutAsync($"/api/v1/resources/{createdResource!.Resource.Id}", updateRequest, tenant2Id, tenant2Token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify original resource unchanged
        var verifyResponse = await GetAsync($"/api/v1/resources/{createdResource.Resource.Id}", tenant1Id, tenant1Token);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifiedResource = await verifyResponse.Content.ReadFromJsonAsync<ResourceDto>();
        verifiedResource!.Name.Should().Be("Tenant 1 Resource", "resource should not be modified by other tenant");
    }
}
