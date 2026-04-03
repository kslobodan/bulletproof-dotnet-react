using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Commands.DeleteResource;

/// <summary>
/// Handler for DeleteResourceCommand
/// </summary>
public class DeleteResourceCommandHandler : IRequestHandler<DeleteResourceCommand, DeleteResourceResponse>
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ITenantContext _tenantContext;

    public DeleteResourceCommandHandler(
        IResourceRepository resourceRepository,
        ITenantContext tenantContext)
    {
        _resourceRepository = resourceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<DeleteResourceResponse> Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
    {
        // Validate tenant context is set
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        var tenantId = _tenantContext.TenantId;

        // Get existing resource (repository filters by tenant automatically)
        var resource = await _resourceRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Resource with ID {request.Id} not found");

        // Delete resource
        await _resourceRepository.DeleteAsync(request.Id);

        return new DeleteResourceResponse
        {
            Message = "Resource deleted successfully"
        };
    }
}
