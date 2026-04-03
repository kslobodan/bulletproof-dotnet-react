using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Queries.GetResourceById;

/// <summary>
/// Handler for GetResourceByIdQuery
/// </summary>
public class GetResourceByIdQueryHandler : IRequestHandler<GetResourceByIdQuery, ResourceDto>
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ITenantContext _tenantContext;

    public GetResourceByIdQueryHandler(
        IResourceRepository resourceRepository,
        ITenantContext tenantContext)
    {
        _resourceRepository = resourceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ResourceDto> Handle(GetResourceByIdQuery request, CancellationToken cancellationToken)
    {
        // Validate tenant context is set
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        // Get resource (repository filters by tenant automatically)
        var resource = await _resourceRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Resource with ID {request.Id} not found");

        // Map to DTO
        return new ResourceDto
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
    }
}
