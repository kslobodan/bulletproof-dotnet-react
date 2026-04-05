using BookingSystem.Application.Common.Interfaces;
using MediatR;

namespace BookingSystem.Application.Features.AuditLogs.Queries.GetPaginatedAuditLogs;

public class GetPaginatedAuditLogsQueryHandler : IRequestHandler<GetPaginatedAuditLogsQuery, GetPaginatedAuditLogsResponse>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetPaginatedAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<GetPaginatedAuditLogsResponse> Handle(GetPaginatedAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var pagedAuditLogs = await _auditLogRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.EntityName,
            request.EntityId,
            request.Action,
            request.UserId,
            request.FromDate,
            request.ToDate,
            cancellationToken
        );

        var auditLogDtos = pagedAuditLogs.Items.Select(log => new AuditLogDto
        {
            Id = log.Id,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            Action = log.Action,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            UserId = log.UserId,
            TenantId = log.TenantId,
            Timestamp = log.Timestamp
        }).ToList();

        return new GetPaginatedAuditLogsResponse
        {
            AuditLogs = new Common.Models.PagedResult<AuditLogDto>(
                auditLogDtos,
                pagedAuditLogs.TotalCount,
                pagedAuditLogs.PageNumber,
                pagedAuditLogs.PageSize
            )
        };
    }
}
