using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.AvailabilityRules.Commands.DeleteAvailabilityRule;

public class DeleteAvailabilityRuleCommandHandler : IRequestHandler<DeleteAvailabilityRuleCommand, DeleteAvailabilityRuleResponse>
{
    private readonly IAvailabilityRuleRepository _availabilityRuleRepository;

    public DeleteAvailabilityRuleCommandHandler(IAvailabilityRuleRepository availabilityRuleRepository)
    {
        _availabilityRuleRepository = availabilityRuleRepository;
    }

    public async Task<DeleteAvailabilityRuleResponse> Handle(DeleteAvailabilityRuleCommand request, CancellationToken cancellationToken)
    {
        var availabilityRule = await _availabilityRuleRepository.GetByIdAsync(request.Id);
        if (availabilityRule == null)
        {
            throw new KeyNotFoundException($"Availability rule with ID {request.Id} not found");
        }

        // Soft delete (marks as deleted, preserves data)
        await _availabilityRuleRepository.SoftDeleteAsync(request.Id);

        return new DeleteAvailabilityRuleResponse
        {
            Message = "Availability rule deleted successfully"
        };
    }
}
