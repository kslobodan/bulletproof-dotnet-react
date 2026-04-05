using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using FluentAssertions;

namespace BookingSystem.UnitTests.Domain.Entities;

public class BookingTests
{
    [Fact]
    public void Booking_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var booking = new Booking();

        // Assert
        booking.IsDeleted.Should().BeFalse();
        booking.DeletedAt.Should().BeNull();
        booking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public void Booking_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);
        var createdAt = DateTime.UtcNow;

        // Act
        var booking = new Booking
        {
            Id = id,
            TenantId = tenantId,
            ResourceId = resourceId,
            UserId = userId,
            StartTime = startTime,
            EndTime = endTime,
            Status = BookingStatus.Confirmed,
            Title = "Meeting Room Booking",
            Description = "Team planning session",
            Notes = "Bring laptop",
            CreatedAt = createdAt,
            CreatedBy = userId
        };

        // Assert
        booking.Id.Should().Be(id);
        booking.TenantId.Should().Be(tenantId);
        booking.ResourceId.Should().Be(resourceId);
        booking.UserId.Should().Be(userId);
        booking.StartTime.Should().Be(startTime);
        booking.EndTime.Should().Be(endTime);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.Title.Should().Be("Meeting Room Booking");
        booking.Description.Should().Be("Team planning session");
        booking.Notes.Should().Be("Bring laptop");
        booking.CreatedAt.Should().Be(createdAt);
        booking.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public void Booking_ShouldSupportSoftDelete()
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = BookingStatus.Confirmed
        };

        var deletedAt = DateTime.UtcNow;

        // Act
        booking.IsDeleted = true;
        booking.DeletedAt = deletedAt;

        // Assert
        booking.IsDeleted.Should().BeTrue();
        booking.DeletedAt.Should().Be(deletedAt);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Rejected)]
    public void Booking_ShouldSupportAllStatusValues(BookingStatus status)
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
        };

        // Act
        booking.Status = status;

        // Assert
        booking.Status.Should().Be(status);
    }

    [Fact]
    public void Booking_ShouldAllowNullableOptionalProperties()
    {
        // Arrange & Act
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            Title = null,
            Description = null,
            Notes = null,
            UpdatedAt = null,
            CreatedBy = null,
            UpdatedBy = null,
            DeletedAt = null
        };

        // Assert
        booking.Title.Should().BeNull();
        booking.Description.Should().BeNull();
        booking.Notes.Should().BeNull();
        booking.UpdatedAt.Should().BeNull();
        booking.CreatedBy.Should().BeNull();
        booking.UpdatedBy.Should().BeNull();
        booking.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Booking_ShouldTrackUpdateMetadata()
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var updatedAt = DateTime.UtcNow;
        var updatedBy = Guid.NewGuid();

        // Act
        booking.UpdatedAt = updatedAt;
        booking.UpdatedBy = updatedBy;

        // Assert
        booking.UpdatedAt.Should().Be(updatedAt);
        booking.UpdatedBy.Should().Be(updatedBy);
    }
}
