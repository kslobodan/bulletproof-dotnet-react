using FluentValidation;

namespace BookingSystem.Application.Features.AvailabilityRules.Queries.GetAllAvailabilityRules;

public class GetAllAvailabilityRulesQueryValidator : AbstractValidator<GetAllAvailabilityRulesQuery>
{
    public GetAllAvailabilityRulesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");

        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("Invalid day of week")
            .When(x => x.DayOfWeek.HasValue);
    }
}
