using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Commands.UpdateResource;

/// <summary>
/// Handler for UpdateResourceCommand
/// </summary>
public class UpdateResourceCommandHandler : IRequestHandler<UpdateResourceCommand, UpdateResourceResponse>
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ITenantContext _tenantContext;

    public UpdateResourceCommandHandler(
        IResourceRepository resourceRepository,
        ITenantContext tenantContext)
    {
        _resourceRepository = resourceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<UpdateResourceResponse> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        // Validate tenant context is set
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        var tenantId = _tenantContext.TenantId;

        // Get existing resource (repository filters by tenant automatically)
        var resource = await _resourceRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Resource with ID {request.Id} not found");

        // Update properties
        resource.Name = request.Name;
        resource.Description = request.Description;
        resource.ResourceType = request.ResourceType;
        resource.Capacity = request.Capacity;
        resource.IsActive = request.IsActive;
        resource.UpdatedAt = DateTime.UtcNow;

        // Save changes
        await _resourceRepository.UpdateAsync(resource);

        // Map to DTO
        var resourceDto = new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            ResourceType = resource.ResourceType,
            Capacity = resource.Capacity,
            IsActive = resource.IsActive,
            TenantId = resource.TenantId,
            CreatedAt = resource.CreatedAt,
            UpdatedAt = resource.UpdatedAt
        };

        return new UpdateResourceResponse
        {
            Resource = resourceDto,
            Message = "Resource updated successfully"
        };
    }
}
