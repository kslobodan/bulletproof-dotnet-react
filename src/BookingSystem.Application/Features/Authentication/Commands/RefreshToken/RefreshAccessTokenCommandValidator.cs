using FluentValidation;

namespace BookingSystem.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Validator for RefreshAccessTokenCommand.
/// Ensures refresh token is provided.
/// </summary>
public class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required");
    }
}
