using BookingSystem.Application.Common.Models;
using MediatR;

namespace BookingSystem.Application.Features.AuditLogs.Queries.GetPaginatedAuditLogs;

public class GetPaginatedAuditLogsQuery : IRequest<GetPaginatedAuditLogsResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string? Action { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class GetPaginatedAuditLogsResponse
{
    public PagedResult<AuditLogDto> AuditLogs { get; set; } = null!;
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime Timestamp { get; set; }
}
