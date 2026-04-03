using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Resources.DTOs;
using BookingSystem.Domain.Entities;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Commands.CreateResource;

/// <summary>
/// Handler for CreateResourceCommand
/// </summary>
public class CreateResourceCommandHandler : IRequestHandler<CreateResourceCommand, CreateResourceResponse>
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ITenantContext _tenantContext;

    public CreateResourceCommandHandler(
        IResourceRepository resourceRepository,
        ITenantContext tenantContext)
    {
        _resourceRepository = resourceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateResourceResponse> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
    {
        // Validate tenant context is set
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        var tenantId = _tenantContext.TenantId;

        // Create resource entity
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ResourceType = request.ResourceType,
            Capacity = request.Capacity,
            IsActive = true,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database (repository handles tenant isolation)
        await _resourceRepository.AddAsync(resource);

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

        return new CreateResourceResponse
        {
            Resource = resourceDto,
            Message = "Resource created successfully"
        };
    }
}
