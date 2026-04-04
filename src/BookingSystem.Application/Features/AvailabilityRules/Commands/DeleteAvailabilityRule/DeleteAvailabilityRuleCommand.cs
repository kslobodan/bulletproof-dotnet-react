using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Commands.DeleteAvailabilityRule;

public class DeleteAvailabilityRuleCommand : IRequest<DeleteAvailabilityRuleResponse>
{
    public Guid Id { get; set; }
}
