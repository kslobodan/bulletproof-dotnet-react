using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Queries.GetAvailabilityRuleById;

public class GetAvailabilityRuleByIdQuery : IRequest<AvailabilityRuleDto>
{
    public Guid Id { get; set; }
}
