using BookingSystem.Application.Common.Models;
using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Queries.GetAllAvailabilityRules;

public class GetAllAvailabilityRulesQuery : IRequest<PagedResult<AvailabilityRuleDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // Optional filters
    public Guid? ResourceId { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public bool? IsActive { get; set; }
}
