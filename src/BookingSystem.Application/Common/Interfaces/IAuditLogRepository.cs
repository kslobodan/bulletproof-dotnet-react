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
        string? action = null,
        DateTime? startDate = null,
        DateTime? endDate = null);
    Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId, int limit = 50);
}
