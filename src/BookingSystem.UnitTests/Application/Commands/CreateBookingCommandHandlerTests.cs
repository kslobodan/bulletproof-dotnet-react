using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Bookings.Commands.CreateBooking;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using FluentAssertions;
using Moq;

namespace BookingSystem.UnitTests.Application.Commands;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IResourceRepository> _resourceRepositoryMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly CreateBookingCommandHandler _handler;

    public CreateBookingCommandHandlerTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _resourceRepositoryMock = new Mock<IResourceRepository>();
        _tenantContextMock = new Mock<ITenantContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new CreateBookingCommandHandler(
            _bookingRepositoryMock.Object,
            _resourceRepositoryMock.Object,
            _tenantContextMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateBooking_WhenRequestIsValid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);

        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var resource = new Resource
        {
            Id = resourceId,
            TenantId = tenantId,
            Name = "Test Room",
            IsActive = true
        };

        _resourceRepositoryMock
            .Setup(x => x.GetByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        _bookingRepositoryMock
            .Setup(x => x.HasConflictAsync(resourceId, startTime, endTime, null))
            .ReturnsAsync(false);

        _bookingRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var command = new CreateBookingCommand
        {
            ResourceId = resourceId,
            StartTime = startTime,
            EndTime = endTime,
            Title = "Team Meeting",
            Description = "Weekly sync"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Booking.Should().NotBeNull();
        result.Booking.ResourceId.Should().Be(resourceId);
        result.Booking.UserId.Should().Be(userId);
        result.Booking.TenantId.Should().Be(tenantId);
        result.Booking.StartTime.Should().Be(startTime);
        result.Booking.EndTime.Should().Be(endTime);
        result.Booking.Title.Should().Be("Team Meeting");
        result.Booking.Description.Should().Be("Weekly sync");
        result.Booking.Status.Should().Be(BookingStatus.Pending);
        result.Message.Should().Be("Booking created successfully");

        _bookingRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Booking>(b =>
                b.ResourceId == resourceId &&
                b.UserId == userId &&
                b.Status == BookingStatus.Pending), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenTenantContextIsNotResolved()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.IsResolved).Returns(false);

        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Tenant context is required");

        _resourceRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenResourceDoesNotExist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        _resourceRepositoryMock
            .Setup(x => x.GetByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        var command = new CreateBookingCommand
        {
            ResourceId = resourceId,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Resource with ID {resourceId} not found");

        _bookingRepositoryMock.Verify(
            x => x.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenResourceIsInactive()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        var inactiveResource = new Resource
        {
            Id = resourceId,
            TenantId = tenantId,
            Name = "Inactive Room",
            IsActive = false // Inactive
        };

        _resourceRepositoryMock
            .Setup(x => x.GetByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveResource);

        var command = new CreateBookingCommand
        {
            ResourceId = resourceId,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot book an inactive resource");

        _bookingRepositoryMock.Verify(
            x => x.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenBookingConflictExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddDays(1);
        var endTime = startTime.AddHours(2);

        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        var resource = new Resource
        {
            Id = resourceId,
            TenantId = tenantId,
            Name = "Test Room",
            IsActive = true
        };

        _resourceRepositoryMock
            .Setup(x => x.GetByIdAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        _bookingRepositoryMock
            .Setup(x => x.HasConflictAsync(resourceId, startTime, endTime, null))
            .ReturnsAsync(true); // Conflict exists!

        var command = new CreateBookingCommand
        {
            ResourceId = resourceId,
            StartTime = startTime,
            EndTime = endTime,
            Title = "Team Meeting"
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This time slot is already booked");

        _bookingRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSetStatusToPending_ByDefault()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var resource = new Resource { Id = resourceId, IsActive = true };
        _resourceRepositoryMock.Setup(x => x.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookingRepositoryMock.Setup(x => x.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(false);
        _bookingRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        var command = new CreateBookingCommand
        {
            ResourceId = resourceId,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Booking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task Handle_ShouldSetCreatedByToCurrentUser()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var resource = new Resource { Id = resourceId, IsActive = true };
        _resourceRepositoryMock.Setup(x => x.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);
        _bookingRepositoryMock.Setup(x => x.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(false);
        _bookingRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        var command = new CreateBookingCommand
        {
            ResourceId = resourceId,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _bookingRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Booking>(b => b.CreatedBy == userId), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
