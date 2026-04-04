using FluentValidation;

namespace BookingSystem.Application.Features.Bookings.Commands.UpdateBooking;

/// <summary>
/// Validator for UpdateBookingCommand
/// </summary>
public class UpdateBookingCommandValidator : AbstractValidator<UpdateBookingCommand>
{
    public UpdateBookingCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("BookingId is required");

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("StartTime must be in the future")
            .When(x => x.StartTime.HasValue);

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime ?? DateTime.MinValue)
            .WithMessage("EndTime must be after StartTime")
            .When(x => x.EndTime.HasValue && x.StartTime.HasValue);

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
