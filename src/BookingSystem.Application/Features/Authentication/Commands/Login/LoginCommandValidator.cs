using FluentValidation;

namespace BookingSystem.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Validator for LoginCommand.
/// Validates login credentials format before authentication.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
