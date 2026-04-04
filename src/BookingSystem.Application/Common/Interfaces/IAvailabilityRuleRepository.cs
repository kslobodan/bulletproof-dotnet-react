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
}
