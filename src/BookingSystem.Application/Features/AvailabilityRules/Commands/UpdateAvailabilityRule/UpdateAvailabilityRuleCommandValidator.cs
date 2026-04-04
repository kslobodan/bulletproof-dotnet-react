using FluentValidation;

namespace BookingSystem.Application.Features.AvailabilityRules.Commands.UpdateAvailabilityRule;

public class UpdateAvailabilityRuleCommandValidator : AbstractValidator<UpdateAvailabilityRuleCommand>
{
    public UpdateAvailabilityRuleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required")
            .Must(BeValidTimeSpan).WithMessage("StartTime must be between 00:00 and 23:59");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("EndTime is required")
            .Must(BeValidTimeSpan).WithMessage("EndTime must be between 00:00 and 23:59")
            .GreaterThan(x => x.StartTime).WithMessage("EndTime must be after StartTime");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveFrom.HasValue && x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be after EffectiveFrom");
    }

    private bool BeValidTimeSpan(TimeSpan time)
    {
        return time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);
    }
}
