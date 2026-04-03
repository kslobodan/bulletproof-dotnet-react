using Asp.Versioning;
using BookingSystem.Application.Features.Resources.Commands.CreateResource;
using BookingSystem.Application.Features.Resources.Commands.UpdateResource;
using BookingSystem.Application.Features.Resources.Commands.DeleteResource;
using BookingSystem.Application.Features.Resources.Queries.GetResourceById;
using BookingSystem.Application.Features.Resources.Queries.GetAllResources;
using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers.v1;

/// <summary>
/// Resources management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResourcesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all resources with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="resourceType">Optional filter by resource type</param>
    /// <param name="isActive">Optional filter by active status</param>
    /// <returns>Paginated list of resources</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? resourceType = null,
        [FromQuery] bool? isActive = null)
    {
        var query = new GetAllResourcesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ResourceType = resourceType,
            IsActive = isActive
        };

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Get a specific resource by ID
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <returns>Resource details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetResourceByIdQuery { Id = id };

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Create a new resource
    /// </summary>
    /// <param name="request">Resource creation details</param>
    /// <returns>Created resource</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateResourceRequest request)
    {
        var command = new CreateResourceCommand
        {
            Name = request.Name,
            Description = request.Description,
            ResourceType = request.ResourceType,
            Capacity = request.Capacity
        };

        var result = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Resource.Id },
            result);
    }

    /// <summary>
    /// Update an existing resource
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <param name="request">Updated resource details</param>
    /// <returns>Updated resource</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UpdateResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResourceRequest request)
    {
        var command = new UpdateResourceCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            ResourceType = request.ResourceType,
            Capacity = request.Capacity,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Delete a resource
    /// </summary>
    /// <param name="id">Resource ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DeleteResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteResourceCommand { Id = id };

        var result = await _mediator.Send(command);

        return Ok(result);
    }
}
