using FluentValidation;

namespace BookingSystem.Application.Features.AvailabilityRules.Commands.DeleteAvailabilityRule;

public class DeleteAvailabilityRuleCommandValidator : AbstractValidator<DeleteAvailabilityRuleCommand>
{
    public DeleteAvailabilityRuleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");
    }
}
