using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Models;
using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Queries.GetAllResources;

/// <summary>
/// Handler for GetAllResourcesQuery
/// </summary>
public class GetAllResourcesQueryHandler : IRequestHandler<GetAllResourcesQuery, PagedResult<ResourceDto>>
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ITenantContext _tenantContext;

    public GetAllResourcesQueryHandler(
        IResourceRepository resourceRepository,
        ITenantContext tenantContext)
    {
        _resourceRepository = resourceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<ResourceDto>> Handle(GetAllResourcesQuery request, CancellationToken cancellationToken)
    {
        // Validate tenant context is set
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        // Get paginated resources (repository filters by tenant automatically)
        var pagedResources = await _resourceRepository.GetPagedAsync(
            request.PageNumber, 
            request.PageSize);

        // Map entities to DTOs
        var resourceDtos = pagedResources.Items.Select(resource => new ResourceDto
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
        }).ToList();

        return new PagedResult<ResourceDto>(
            resourceDtos,
            pagedResources.TotalCount,
            pagedResources.PageNumber,
            pagedResources.PageSize
        );
    }
}
