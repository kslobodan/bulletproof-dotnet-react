using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Resources.Commands.CreateResource;
using BookingSystem.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BookingSystem.UnitTests.Application.Commands;

public class CreateResourceCommandHandlerTests
{
    private readonly Mock<IResourceRepository> _resourceRepositoryMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly CreateResourceCommandHandler _handler;

    public CreateResourceCommandHandlerTests()
    {
        _resourceRepositoryMock = new Mock<IResourceRepository>();
        _tenantContextMock = new Mock<ITenantContext>();
        _handler = new CreateResourceCommandHandler(
            _resourceRepositoryMock.Object,
            _tenantContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateResource_WhenRequestIsValid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = new CreateResourceCommand
        {
            Name = "Conference Room A",
            Description = "Large conference room",
            ResourceType = "Room",
            Capacity = 20
        };

        _resourceRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Resource.Should().NotBeNull();
        result.Resource.Name.Should().Be("Conference Room A");
        result.Resource.Description.Should().Be("Large conference room");
        result.Resource.ResourceType.Should().Be("Room");
        result.Resource.Capacity.Should().Be(20);
        result.Resource.IsActive.Should().BeTrue();
        result.Resource.TenantId.Should().Be(tenantId);
        result.Message.Should().Be("Resource created successfully");

        _resourceRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Resource>(r =>
                r.Name == "Conference Room A" &&
                r.TenantId == tenantId &&
                r.IsActive == true), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenTenantContextIsNotResolved()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.IsResolved).Returns(false);

        var command = new CreateResourceCommand
        {
            Name = "Conference Room A",
            ResourceType = "Room",
            Capacity = 20
        };

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Tenant context is required");

        _resourceRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldGenerateNewGuid_ForResourceId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = new CreateResourceCommand
        {
            Name = "Test Resource",
            ResourceType = "Equipment",
            Capacity = 1
        };

        _resourceRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Resource.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldSetCreatedAtToUtcNow()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = new CreateResourceCommand
        {
            Name = "Test Resource",
            ResourceType = "Equipment",
            Capacity = 1
        };

        _resourceRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var beforeExecution = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        var afterExecution = DateTime.UtcNow;

        // Assert
        result.Resource.CreatedAt.Should().BeOnOrAfter(beforeExecution);
        result.Resource.CreatedAt.Should().BeOnOrBefore(afterExecution);
    }

    [Fact]
    public async Task Handle_ShouldSetIsActiveToTrue_ByDefault()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContextMock.Setup(x => x.IsResolved).Returns(true);
        _tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = new CreateResourceCommand
        {
            Name = "Test Resource",
            ResourceType = "Equipment",
            Capacity = 1
        };

        _resourceRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Resource.IsActive.Should().BeTrue();
    }
}
