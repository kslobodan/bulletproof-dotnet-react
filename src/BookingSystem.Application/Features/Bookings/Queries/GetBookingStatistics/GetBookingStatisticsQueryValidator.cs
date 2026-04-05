using FluentValidation;

namespace BookingSystem.Application.Features.Bookings.Queries.GetBookingStatistics;

public class GetBookingStatisticsQueryValidator : AbstractValidator<GetBookingStatisticsQuery>
{
    public GetBookingStatisticsQueryValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be greater than or equal to StartDate");

        RuleFor(x => x.TopResourcesCount)
            .InclusiveBetween(1, 50)
            .WithMessage("TopResourcesCount must be between 1 and 50");
    }
}
