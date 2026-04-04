using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Queries.GetAvailabilityRuleById;

public class GetAvailabilityRuleByIdQueryHandler : IRequestHandler<GetAvailabilityRuleByIdQuery, AvailabilityRuleDto>
{
    private readonly IAvailabilityRuleRepository _availabilityRuleRepository;
    private readonly ITenantContext _tenantContext;

    public GetAvailabilityRuleByIdQueryHandler(
        IAvailabilityRuleRepository availabilityRuleRepository,
        ITenantContext tenantContext)
    {
        _availabilityRuleRepository = availabilityRuleRepository;
        _tenantContext = tenantContext;
    }

    public async Task<AvailabilityRuleDto> Handle(GetAvailabilityRuleByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required for this operation");
        }

        var availabilityRule = await _availabilityRuleRepository.GetByIdAsync(request.Id);
        
        if (availabilityRule == null)
        {
            throw new KeyNotFoundException($"Availability rule with ID {request.Id} not found");
        }

        // Convert to DTO with computed fields
        return new AvailabilityRuleDto
        {
            Id = availabilityRule.Id,
            TenantId = availabilityRule.TenantId,
            ResourceId = availabilityRule.ResourceId,
            ResourceName = string.Empty, // Will be populated by controller if needed
            DayOfWeek = availabilityRule.DayOfWeek,
            DayOfWeekText = availabilityRule.DayOfWeek.ToString(),
            StartTime = availabilityRule.StartTime,
            StartTimeText = availabilityRule.StartTime.ToString(@"hh\:mm"),
            EndTime = availabilityRule.EndTime,
            EndTimeText = availabilityRule.EndTime.ToString(@"hh\:mm"),
            DurationMinutes = (int)(availabilityRule.EndTime - availabilityRule.StartTime).TotalMinutes,
            IsActive = availabilityRule.IsActive,
            EffectiveFrom = availabilityRule.EffectiveFrom,
            EffectiveTo = availabilityRule.EffectiveTo,
            CreatedAt = availabilityRule.CreatedAt,
            UpdatedAt = availabilityRule.UpdatedAt
        };
    }
}
