using BookingSystem.Application.Features.Authentication.Commands.Login;
using FluentAssertions;

namespace BookingSystem.UnitTests.Application.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator;

    public LoginCommandValidatorTests()
    {
        _validator = new LoginCommandValidator();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailIsEmpty()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "",
            Password = "SecurePassword123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage == "Email is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailIsNull()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = null!,
            Password = "SecurePassword123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain.com")]
    [InlineData("user@")]
    public void Validate_ShouldFail_WhenEmailFormatIsInvalid(string invalidEmail)
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = invalidEmail,
            Password = "SecurePassword123!"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage == "Invalid email format");
    }

    [Fact]
    public void Validate_ShouldPass_WithValidEmailFormats()
    {
        // Arrange
        var validEmails = new[]
        {
            "user@example.com",
            "test.user@example.co.uk",
            "user+tag@example.com",
            "user123@test-domain.com"
        };

        foreach (var email in validEmails)
        {
            var command = new LoginCommand
            {
                Email = email,
                Password = "Password123"
            };

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue($"Email '{email}' should be valid");
        }
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsEmpty()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = ""
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage == "Password is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsNull()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = null!
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage == "Password is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenBothEmailAndPasswordAreEmpty()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "",
            Password = ""
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3); // Email required, Invalid email format, Password required
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}
