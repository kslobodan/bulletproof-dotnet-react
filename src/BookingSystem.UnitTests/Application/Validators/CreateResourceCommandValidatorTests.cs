using BookingSystem.Application.Features.Resources.Commands.CreateResource;
using FluentAssertions;

namespace BookingSystem.UnitTests.Application.Validators;

public class CreateResourceCommandValidatorTests
{
    private readonly CreateResourceCommandValidator _validator;

    public CreateResourceCommandValidatorTests()
    {
        _validator = new CreateResourceCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "Conference Room A",
            ResourceType = "Room",
            Description = "Large conference room with projector",
            Capacity = 20
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WhenOptionalFieldsAreNull()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "Desk 1",
            ResourceType = "Desk",
            Description = null,
            Capacity = null
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "",
            ResourceType = "Room"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Resource name is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsTooShort()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "A",
            ResourceType = "Room"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Resource name must be at least 2 characters");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNameIsTooLong()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = new string('A', 201),
            ResourceType = "Room"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage == "Resource name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_ShouldFail_WhenResourceTypeIsEmpty()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "Conference Room",
            ResourceType = ""
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResourceType" && e.ErrorMessage == "Resource type is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenResourceTypeIsTooLong()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "Conference Room",
            ResourceType = new string('A', 101)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResourceType" && e.ErrorMessage == "Resource type must not exceed 100 characters");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionIsTooLong()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "Conference Room",
            ResourceType = "Room",
            Description = new string('A', 1001)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage == "Description must not exceed 1000 characters");
    }

    [Fact]
    public void Validate_ShouldFail_WhenCapacityIsZero()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "Conference Room",
            ResourceType = "Room",
            Capacity = 0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Capacity" && e.ErrorMessage == "Capacity must be greater than 0");
    }

    [Fact]
    public void Validate_ShouldFail_WhenCapacityIsNegative()
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = "Conference Room",
            ResourceType = "Room",
            Capacity = -5
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Capacity" && e.ErrorMessage == "Capacity must be greater than 0");
    }
}
