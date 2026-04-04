using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using BookingSystem.Domain.Entities;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Commands.CreateAvailabilityRule;

public class CreateAvailabilityRuleCommandHandler : IRequestHandler<CreateAvailabilityRuleCommand, CreateAvailabilityRuleResponse>
{
    private readonly IAvailabilityRuleRepository _availabilityRuleRepository;
    private readonly ITenantContext _tenantContext;

    public CreateAvailabilityRuleCommandHandler(
        IAvailabilityRuleRepository availabilityRuleRepository,
        ITenantContext tenantContext)
    {
        _availabilityRuleRepository = availabilityRuleRepository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateAvailabilityRuleResponse> Handle(CreateAvailabilityRuleCommand request, CancellationToken cancellationToken)
    {
        var availabilityRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ResourceId = request.ResourceId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsActive = request.IsActive,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            CreatedAt = DateTime.UtcNow
        };

        await _availabilityRuleRepository.AddAsync(availabilityRule);

        var dto = new AvailabilityRuleDto
        {
            Id = availabilityRule.Id,
            TenantId = availabilityRule.TenantId,
            ResourceId = availabilityRule.ResourceId,
            ResourceName = string.Empty, // Will be populated by AutoMapper if needed
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

        return new CreateAvailabilityRuleResponse
        {
            AvailabilityRule = dto,
            Message = "Availability rule created successfully"
        };
    }
}
