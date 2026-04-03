using FluentValidation;

namespace BookingSystem.Application.Features.Resources.Commands.UpdateResource;

/// <summary>
/// Validator for UpdateResourceCommand
/// </summary>
public class UpdateResourceCommandValidator : AbstractValidator<UpdateResourceCommand>
{
    public UpdateResourceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Resource ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Resource name is required")
            .MinimumLength(2).WithMessage("Resource name must be at least 2 characters")
            .MaximumLength(200).WithMessage("Resource name must not exceed 200 characters");

        RuleFor(x => x.ResourceType)
            .NotEmpty().WithMessage("Resource type is required")
            .MaximumLength(100).WithMessage("Resource type must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0")
            .When(x => x.Capacity.HasValue);
    }
}
