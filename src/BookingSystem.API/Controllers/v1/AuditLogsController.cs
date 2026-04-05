using BookingSystem.Application.Features.AuditLogs.Queries.GetPaginatedAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireTenantAdmin")]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated audit logs with optional filters (TenantAdmin only)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetPaginatedAuditLogsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? entityName = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetPaginatedAuditLogsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
