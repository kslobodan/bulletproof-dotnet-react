using BookingSystem.Application.Common.Models;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid entityId);
    Task<PagedResult<AuditLog>> GetPagedAsync(
        int pageNumber, 
        int pageSize,
        string? entityName = null,
        Guid? entityId = null,
        string? action = null,
        Guid? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId, int limit = 50);
}
