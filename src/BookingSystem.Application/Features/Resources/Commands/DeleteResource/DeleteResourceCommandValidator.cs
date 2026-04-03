using FluentValidation;

namespace BookingSystem.Application.Features.Resources.Commands.DeleteResource;

/// <summary>
/// Validator for DeleteResourceCommand
/// </summary>
public class DeleteResourceCommandValidator : AbstractValidator<DeleteResourceCommand>
{
    public DeleteResourceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Resource ID is required");
    }
}
