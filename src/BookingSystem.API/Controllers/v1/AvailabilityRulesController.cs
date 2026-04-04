using Asp.Versioning;
using BookingSystem.Application.Features.AvailabilityRules.Commands.CreateAvailabilityRule;
using BookingSystem.Application.Features.AvailabilityRules.Commands.UpdateAvailabilityRule;
using BookingSystem.Application.Features.AvailabilityRules.Commands.DeleteAvailabilityRule;
using BookingSystem.Application.Features.AvailabilityRules.Queries.GetAvailabilityRuleById;
using BookingSystem.Application.Features.AvailabilityRules.Queries.GetAllAvailabilityRules;
using BookingSystem.Application.Features.AvailabilityRules.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers.v1;

/// <summary>
/// Availability rules management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class AvailabilityRulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AvailabilityRulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all availability rules with pagination and filtering
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="resourceId">Optional filter by resource</param>
    /// <param name="dayOfWeek">Optional filter by day of week (0=Sunday, 1=Monday, ..., 6=Saturday)</param>
    /// <param name="isActive">Optional filter by active status</param>
    /// <returns>Paginated list of availability rules</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? resourceId = null,
        [FromQuery] DayOfWeek? dayOfWeek = null,
        [FromQuery] bool? isActive = null)
    {
        var query = new GetAllAvailabilityRulesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ResourceId = resourceId,
            DayOfWeek = dayOfWeek,
            IsActive = isActive
        };

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Get a specific availability rule by ID
    /// </summary>
    /// <param name="id">Availability rule ID</param>
    /// <returns>Availability rule details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AvailabilityRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetAvailabilityRuleByIdQuery { Id = id };

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Create a new availability rule
    /// </summary>
    /// <param name="request">Availability rule creation details</param>
    /// <returns>Created availability rule</returns>
    [HttpPost]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(CreateAvailabilityRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateAvailabilityRuleRequest request)
    {
        var command = new CreateAvailabilityRuleCommand
        {
            ResourceId = request.ResourceId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsActive = request.IsActive,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo
        };

        var result = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.AvailabilityRule.Id },
            result);
    }

    /// <summary>
    /// Update an existing availability rule
    /// </summary>
    /// <param name="id">Availability rule ID</param>
    /// <param name="request">Updated availability rule details</param>
    /// <returns>Updated availability rule</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(UpdateAvailabilityRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAvailabilityRuleRequest request)
    {
        var command = new UpdateAvailabilityRuleCommand
        {
            Id = id,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsActive = request.IsActive,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo
        };

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Delete an availability rule
    /// </summary>
    /// <param name="id">Availability rule ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(DeleteAvailabilityRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteAvailabilityRuleCommand { Id = id };

        var result = await _mediator.Send(command);

        return Ok(result);
    }
}
