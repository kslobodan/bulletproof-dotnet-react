using System.Net;
using System.Net.Http.Json;
using BookingSystem.Application.Common.Models;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Application.Features.Resources.DTOs;
using BookingSystem.Domain.Enums;
using BookingSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BookingSystem.IntegrationTests.Controllers;

[Collection("Database")]
public class BookingsControllerTests : IntegrationTestBase
{
    public BookingsControllerTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }

    [Fact]
    public async Task CreateBooking_WithValidData_ShouldReturn201AndBooking()
    {
        // Arrange - Register tenant, create resource
        var tenantResponse = await RegisterTenantAsync("Booking Test Co", "admin@bookingtest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        // Create a resource first
        var resourceRequest = new CreateResourceRequest
        {
            Name = "Conference Room A",
            Description = "Main conference room",
            ResourceType = "MeetingRoom",
            Capacity = 10
        };
        var resourceResponse = await PostAsync("/api/v1/resources", resourceRequest, tenantId, token);
        var resource = await resourceResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        // Create booking
        var bookingRequest = new CreateBookingRequest
        {
            ResourceId = resource!.Resource.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Title = "Team Meeting",
            Description = "Weekly team sync"
        };

        // Act
        var response = await PostAsync("/api/v1/bookings", bookingRequest, tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateBookingResponse>();
        result.Should().NotBeNull();
        result!.Booking.Should().NotBeNull();
        result.Booking.Id.Should().NotBeEmpty();
        result.Booking.ResourceId.Should().Be(bookingRequest.ResourceId);
        result.Booking.Title.Should().Be(bookingRequest.Title);
        result.Booking.Status.Should().Be(BookingStatus.Pending);
        result.Booking.UserId.Should().Be(tenantResponse.AuthResult.UserId);

        // Verify in database
        using var connection = DbConnectionFactory.CreateConnection();
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM bookings WHERE id = @id AND tenantid = @tenantId";
        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = result.Booking.Id;
        cmd.Parameters.Add(idParam);
        var tenantIdParam = cmd.CreateParameter();
        tenantIdParam.ParameterName = "@tenantId";
        tenantIdParam.Value = tenantResponse.AuthResult.TenantId;
        cmd.Parameters.Add(tenantIdParam);
        var count = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        count.Should().Be(1, "booking should be persisted in database");
    }

    [Fact]
    public async Task CreateBooking_WithOverlappingTimes_ShouldReturn400Conflict()
    {
        // Arrange - Create resource and first booking
        var tenantResponse = await RegisterTenantAsync("Conflict Test Co", "admin@conflicttest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var resourceRequest = new CreateResourceRequest
        {
            Name = "Meeting Room B",
            ResourceType = "MeetingRoom",
            Capacity = 5
        };
        var resourceResponse = await PostAsync("/api/v1/resources", resourceRequest, tenantId, token);
        var resource = await resourceResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(3);

        // Create first booking
        var firstBooking = new CreateBookingRequest
        {
            ResourceId = resource!.Resource.Id,
            StartTime = startTime,
            EndTime = endTime,
            Title = "First Meeting"
        };
        await PostAsync("/api/v1/bookings", firstBooking, tenantId, token);

        // Try to create overlapping booking
        var overlappingBooking = new CreateBookingRequest
        {
            ResourceId = resource.Resource.Id,
            StartTime = startTime.AddMinutes(30), // Overlaps with first booking
            EndTime = endTime.AddMinutes(30),
            Title = "Conflicting Meeting"
        };

        // Act
        var response = await PostAsync("/api/v1/bookings", overlappingBooking, tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("booked");
    }

    [Fact]
    public async Task GetBookingById_WithValidId_ShouldReturn200AndBooking()
    {
        // Arrange - Create booking
        var tenantResponse = await RegisterTenantAsync("GetById Test Co", "admin@getbyidtest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var resourceRequest = new CreateResourceRequest { Name = "Test Resource", ResourceType = "Equipment", Capacity = 1 };
        var resourceResponse = await PostAsync("/api/v1/resources", resourceRequest, tenantId, token);
        var resource = await resourceResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        var bookingRequest = new CreateBookingRequest
        {
            ResourceId = resource!.Resource.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Title = "Test Booking"
        };
        var createResponse = await PostAsync("/api/v1/bookings", bookingRequest, tenantId, token);
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<CreateBookingResponse>();

        // Act
        var response = await GetAsync($"/api/v1/bookings/{createdBooking!.Booking.Id}", tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BookingDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdBooking.Booking.Id);
        result.Title.Should().Be(bookingRequest.Title);
    }

    [Fact]
    public async Task GetBookingById_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var tenantResponse = await RegisterTenantAsync("NotFound Test", "admin@notfoundtest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var invalidId = Guid.NewGuid();

        // Act
        var response = await GetAsync($"/api/v1/bookings/{invalidId}", tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllBookings_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange - Create multiple bookings
        var tenantResponse = await RegisterTenantAsync("Pagination Test", "admin@paginationtest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var resourceRequest = new CreateResourceRequest { Name = "Shared Resource", ResourceType = "MeetingRoom", Capacity = 10 };
        var resourceResponse = await PostAsync("/api/v1/resources", resourceRequest, tenantId, token);
        var resource = await resourceResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        // Create 5 bookings (non-overlapping)
        for (int i = 0; i < 5; i++)
        {
            var bookingRequest = new CreateBookingRequest
            {
                ResourceId = resource!.Resource.Id,
                StartTime = DateTime.UtcNow.AddHours(i * 2),
                EndTime = DateTime.UtcNow.AddHours(i * 2 + 1),
                Title = $"Booking {i + 1}"
            };
            await PostAsync("/api/v1/bookings", bookingRequest, tenantId, token);
        }

        // Act - Get first page with page size 3
        var response = await GetAsync("/api/v1/bookings?pageNumber=1&pageSize=3", tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BookingDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task UpdateBooking_WithValidData_ShouldReturn200AndUpdatedBooking()
    {
        // Arrange - Create booking
        var tenantResponse = await RegisterTenantAsync("Update Test", "admin@updatetest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var resourceRequest = new CreateResourceRequest { Name = "Updatable Resource", ResourceType = "Equipment", Capacity = 1 };
        var resourceResponse = await PostAsync("/api/v1/resources", resourceRequest, tenantId, token);
        var resource = await resourceResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        var bookingRequest = new CreateBookingRequest
        {
            ResourceId = resource!.Resource.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Title = "Original Title"
        };
        var createResponse = await PostAsync("/api/v1/bookings", bookingRequest, tenantId, token);
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<CreateBookingResponse>();

        // Update the booking
        var updateRequest = new UpdateBookingRequest
        {
            StartTime = DateTime.UtcNow.AddHours(3),
            EndTime = DateTime.UtcNow.AddHours(4),
            Title = "Updated Title",
            Description = "Updated description"
        };

        // Act
        var response = await PutAsync($"/api/v1/bookings/{createdBooking!.Booking.Id}", updateRequest, tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateBookingResponse>();
        result.Should().NotBeNull();
        result!.Booking.Title.Should().Be(updateRequest.Title);
        result.Booking.Description.Should().Be(updateRequest.Description);
    }

    [Fact]
    public async Task CancelBooking_WithValidId_ShouldReturn200AndCancelledBooking()
    {
        // Arrange - Create booking
        var tenantResponse = await RegisterTenantAsync("Cancel Test", "admin@canceltest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();
        var token = tenantResponse.AuthResult.Token;

        var resourceRequest = new CreateResourceRequest { Name = "Cancellable Resource", ResourceType = "MeetingRoom", Capacity = 5 };
        var resourceResponse = await PostAsync("/api/v1/resources", resourceRequest, tenantId, token);
        var resource = await resourceResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        var bookingRequest = new CreateBookingRequest
        {
            ResourceId = resource!.Resource.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Title = "Meeting to Cancel"
        };
        var createResponse = await PostAsync("/api/v1/bookings", bookingRequest, tenantId, token);
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<CreateBookingResponse>();

        // Act - Cancel endpoint doesn't require body, use empty object
        var response = await PostAsync($"/api/v1/bookings/{createdBooking!.Booking.Id}/cancel", new { }, tenantId, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CancelBookingResponse>();
        result.Should().NotBeNull();
        result!.BookingId.Should().Be(createdBooking.Booking.Id);
        result.Message.Should().Contain("cancel");

        // Verify status changed in database
        var checkResponse = await GetAsync($"/api/v1/bookings/{createdBooking.Booking.Id}", tenantId, token);
        var booking = await checkResponse.Content.ReadFromJsonAsync<BookingDto>();
        booking!.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task MultiTenant_BookingIsolation_TenantCannotAccessOtherTenantsBookings()
    {
        // Arrange - Create two tenants with bookings
        var tenant1Response = await RegisterTenantAsync("Tenant One", "admin@tenant1.com", "SecurePass123!");
        var tenant1Id = tenant1Response.AuthResult.TenantId.ToString();
        var tenant1Token = tenant1Response.AuthResult.Token;

        var tenant2Response = await RegisterTenantAsync("Tenant Two", "admin@tenant2.com", "SecurePass123!");
        var tenant2Id = tenant2Response.AuthResult.TenantId.ToString();
        var tenant2Token = tenant2Response.AuthResult.Token;

        // Tenant 1 creates resource and booking
        var resourceRequest = new CreateResourceRequest { Name = "Tenant 1 Resource", ResourceType = "MeetingRoom", Capacity = 10 };
        var resourceResponse = await PostAsync("/api/v1/resources", resourceRequest, tenant1Id, tenant1Token);
        var resource = await resourceResponse.Content.ReadFromJsonAsync<CreateResourceResponse>();

        var bookingRequest = new CreateBookingRequest
        {
            ResourceId = resource!.Resource.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Title = "Tenant 1 Booking"
        };
        var createResponse = await PostAsync("/api/v1/bookings", bookingRequest, tenant1Id, tenant1Token);
        var tenant1Booking = await createResponse.Content.ReadFromJsonAsync<CreateBookingResponse>();

        // Act - Tenant 2 tries to access Tenant 1's booking
        var response = await GetAsync($"/api/v1/bookings/{tenant1Booking!.Booking.Id}", tenant2Id, tenant2Token);

        // Assert - Should not find the booking (tenant filtering should prevent access)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBooking_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var tenantResponse = await RegisterTenantAsync("Unauth Test", "admin@unauthtest.com", "SecurePass123!");
        var tenantId = tenantResponse.AuthResult.TenantId.ToString();

        var bookingRequest = new CreateBookingRequest
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Title = "Unauthorized Booking"
        };

        // Act - No token provided
        var response = await PostAsync("/api/v1/bookings", bookingRequest, tenantId, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateBooking_WithoutTenantHeader_ShouldReturn400()
    {
        // Arrange
        var tenantResponse = await RegisterTenantAsync("NoTenant Test", "admin@notenanttest.com", "SecurePass123!");
        var token = tenantResponse.AuthResult.Token;

        var bookingRequest = new CreateBookingRequest
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Title = "No Tenant Booking"
        };

        // Act - No tenant ID provided
        var response = await PostAsync("/api/v1/bookings", bookingRequest, null, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
