using BookingSystem.Application.Common.Models;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Common.Interfaces;

/// <summary>
/// Repository interface for AvailabilityRule entity with tenant-scoped operations
/// </summary>
public interface IAvailabilityRuleRepository : IRepository<AvailabilityRule>
{
    /// <summary>
    /// Gets all availability rules for a specific resource (tenant-filtered).
    /// Useful for checking resource availability schedules.
    /// </summary>
    /// <param name="resourceId">The resource ID to filter by</param>
    /// <returns>Collection of availability rules for the resource</returns>
    Task<IEnumerable<AvailabilityRule>> GetByResourceIdAsync(Guid resourceId);

    /// <summary>
    /// Gets a paginated list of availability rules with optional filters (tenant-filtered).
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="resourceId">Optional resource ID filter</param>
    /// <param name="dayOfWeek">Optional day of week filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <returns>Paginated result of availability rules</returns>
    Task<PagedResult<AvailabilityRule>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? resourceId = null,
        DayOfWeek? dayOfWeek = null,
        bool? isActive = null);
}
