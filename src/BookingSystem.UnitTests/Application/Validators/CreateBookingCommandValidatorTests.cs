using BookingSystem.Application.Features.Bookings.Commands.CreateBooking;
using FluentAssertions;

namespace BookingSystem.UnitTests.Application.Validators;

public class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _validator;

    public CreateBookingCommandValidatorTests()
    {
        _validator = new CreateBookingCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            Title = "Team Meeting",
            Description = "Weekly sync meeting"
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
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Title = null,
            Description = null
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenResourceIdIsEmpty()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.Empty,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResourceId" && e.ErrorMessage == "ResourceId is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenStartTimeIsInThePast()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartTime" && e.ErrorMessage == "StartTime must be in the future");
    }

    [Fact]
    public void Validate_ShouldFail_WhenEndTimeIsBeforeStartTime()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndTime" && e.ErrorMessage == "EndTime must be after StartTime");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDurationIsLessThan15Minutes()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(10) // Only 10 minutes
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Booking must be at least 15 minutes long");
    }

    [Fact]
    public void Validate_ShouldPass_WhenDurationIsExactly15Minutes()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(15) // Exactly 15 minutes
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenDurationExceeds24Hours()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(25) // 25 hours
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Booking cannot exceed 24 hours");
    }

    [Fact]
    public void Validate_ShouldPass_WhenDurationIsExactly24Hours()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(1).Date; // Use exact date to avoid millisecond issues
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = startTime,
            EndTime = startTime.AddHours(24) // Exactly 24 hours
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleIsTooLong()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Title = new string('A', 201)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage == "Title cannot exceed 200 characters");
    }

    [Fact]
    public void Validate_ShouldFail_WhenDescriptionIsTooLong()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ResourceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Description = new string('A', 1001)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage == "Description cannot exceed 1000 characters");
    }
}
