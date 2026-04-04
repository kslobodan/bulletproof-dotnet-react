using FluentValidation;

namespace BookingSystem.Application.Features.AvailabilityRules.Queries.GetAvailabilityRuleById;

public class GetAvailabilityRuleByIdQueryValidator : AbstractValidator<GetAvailabilityRuleByIdQuery>
{
    public GetAvailabilityRuleByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Availability rule ID is required");
    }
}
