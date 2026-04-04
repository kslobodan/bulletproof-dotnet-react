using FluentValidation;

namespace BookingSystem.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Validator for CreateBookingCommand
/// </summary>
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.ResourceId)
            .NotEmpty()
            .WithMessage("ResourceId is required");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("StartTime is required")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("StartTime must be in the future");

        RuleFor(x => x.EndTime)
            .NotEmpty()
            .WithMessage("EndTime is required")
            .GreaterThan(x => x.StartTime)
            .WithMessage("EndTime must be after StartTime");

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters");

        // Business rule: booking duration must be at least 15 minutes
        RuleFor(x => x)
            .Must(x => (x.EndTime - x.StartTime).TotalMinutes >= 15)
            .WithMessage("Booking must be at least 15 minutes long");

        // Business rule: booking cannot exceed 24 hours
        RuleFor(x => x)
            .Must(x => (x.EndTime - x.StartTime).TotalHours <= 24)
            .WithMessage("Booking cannot exceed 24 hours");
    }
}
