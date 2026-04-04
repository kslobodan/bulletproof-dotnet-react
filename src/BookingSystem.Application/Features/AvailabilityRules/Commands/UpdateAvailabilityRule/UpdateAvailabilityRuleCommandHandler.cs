using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Commands.UpdateAvailabilityRule;

public class UpdateAvailabilityRuleCommandHandler : IRequestHandler<UpdateAvailabilityRuleCommand, UpdateAvailabilityRuleResponse>
{
    private readonly IAvailabilityRuleRepository _availabilityRuleRepository;

    public UpdateAvailabilityRuleCommandHandler(IAvailabilityRuleRepository availabilityRuleRepository)
    {
        _availabilityRuleRepository = availabilityRuleRepository;
    }

    public async Task<UpdateAvailabilityRuleResponse> Handle(UpdateAvailabilityRuleCommand request, CancellationToken cancellationToken)
    {
        var availabilityRule = await _availabilityRuleRepository.GetByIdAsync(request.Id);

        if (availabilityRule == null)
        {
            throw new KeyNotFoundException($"Availability rule with ID {request.Id} not found");
        }

        availabilityRule.StartTime = request.StartTime;
        availabilityRule.EndTime = request.EndTime;
        availabilityRule.IsActive = request.IsActive;
        availabilityRule.EffectiveFrom = request.EffectiveFrom;
        availabilityRule.EffectiveTo = request.EffectiveTo;
        availabilityRule.UpdatedAt = DateTime.UtcNow;

        await _availabilityRuleRepository.UpdateAsync(availabilityRule);

        var dto = new AvailabilityRuleDto
        {
            Id = availabilityRule.Id,
            TenantId = availabilityRule.TenantId,
            ResourceId = availabilityRule.ResourceId,
            ResourceName = string.Empty,
            DayOfWeek = availabilityRule.DayOfWeek,
            DayOfWeekText = availabilityRule.DayOfWeek.ToString(),
            StartTime = availabilityRule.StartTime,
            EndTime = availabilityRule.EndTime,
            StartTimeText = availabilityRule.StartTime.ToString(@"hh\:mm"),
            EndTimeText = availabilityRule.EndTime.ToString(@"hh\:mm"),
            IsActive = availabilityRule.IsActive,
            EffectiveFrom = availabilityRule.EffectiveFrom,
            EffectiveTo = availabilityRule.EffectiveTo,
            CreatedAt = availabilityRule.CreatedAt,
            UpdatedAt = availabilityRule.UpdatedAt,
            DurationMinutes = (int)(availabilityRule.EndTime - availabilityRule.StartTime).TotalMinutes
        };

        return new UpdateAvailabilityRuleResponse
        {
            AvailabilityRule = dto,
            Message = "Availability rule updated successfully"
        };
    }
}
