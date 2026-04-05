using FluentValidation;

namespace BookingSystem.Application.Features.Bookings.Queries.GetAllBookings;

public class GetAllBookingsQueryValidator : AbstractValidator<GetAllBookingsQuery>
{
    public GetAllBookingsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("PageNumber must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be greater than or equal to StartDate");

        RuleFor(x => x.OrderBy)
            .Must(orderBy => string.IsNullOrEmpty(orderBy) || 
                  new[] { "StartTime", "EndTime", "CreatedAt", "Status", "Title" }.Contains(orderBy))
            .WithMessage("OrderBy must be one of: StartTime, EndTime, CreatedAt, Status, Title");
    }
}
