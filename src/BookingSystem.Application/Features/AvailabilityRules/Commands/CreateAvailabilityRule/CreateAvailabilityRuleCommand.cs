using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Commands.CreateAvailabilityRule;

public class CreateAvailabilityRuleCommand : IRequest<CreateAvailabilityRuleResponse>
{
    public Guid ResourceId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
