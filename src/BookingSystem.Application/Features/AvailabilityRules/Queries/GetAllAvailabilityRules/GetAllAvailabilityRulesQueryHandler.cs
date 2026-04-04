using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Models;
using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using BookingSystem.Domain.Entities;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Queries.GetAllAvailabilityRules;

public class GetAllAvailabilityRulesQueryHandler : IRequestHandler<GetAllAvailabilityRulesQuery, PagedResult<AvailabilityRuleDto>>
{
    private readonly IAvailabilityRuleRepository _availabilityRuleRepository;
    private readonly ITenantContext _tenantContext;

    public GetAllAvailabilityRulesQueryHandler(
        IAvailabilityRuleRepository availabilityRuleRepository,
        ITenantContext tenantContext)
    {
        _availabilityRuleRepository = availabilityRuleRepository;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<AvailabilityRuleDto>> Handle(GetAllAvailabilityRulesQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required for this operation");
        }

        var pagedResult = await _availabilityRuleRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.ResourceId,
            request.DayOfWeek,
            request.IsActive);

        // Convert entities to DTOs with computed fields
        var dtos = pagedResult.Items.Select(rule => new AvailabilityRuleDto
        {
            Id = rule.Id,
            TenantId = rule.TenantId,
            ResourceId = rule.ResourceId,
            ResourceName = string.Empty, // Will be populated by controller if needed
            DayOfWeek = rule.DayOfWeek,
            DayOfWeekText = rule.DayOfWeek.ToString(),
            StartTime = rule.StartTime,
            StartTimeText = rule.StartTime.ToString(@"hh\:mm"),
            EndTime = rule.EndTime,
            EndTimeText = rule.EndTime.ToString(@"hh\:mm"),
            DurationMinutes = (int)(rule.EndTime - rule.StartTime).TotalMinutes,
            IsActive = rule.IsActive,
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveTo = rule.EffectiveTo,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        }).ToList();

        return new PagedResult<AvailabilityRuleDto>(
            dtos,
            pagedResult.TotalCount,
            pagedResult.PageNumber,
            pagedResult.PageSize);
    }
}
