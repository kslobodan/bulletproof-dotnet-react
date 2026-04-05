using BookingSystem.Application.Common.Models;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Common.Interfaces;

public interface IBookingRepository : IRepository<Booking>
{
    /// <summary>
    /// Checks if there's a booking conflict for the given resource and time range.
    /// Only considers Pending and Confirmed bookings as potential conflicts.
    /// </summary>
    /// <param name="resourceId">The resource to check</param>
    /// <param name="startTime">Start time of the proposed booking</param>
    /// <param name="endTime">End time of the proposed booking</param>
    /// <param name="excludeBookingId">Optional booking ID to exclude from conflict check (for updates)</param>
    /// <returns>True if there's a conflict, false otherwise</returns>
    Task<bool> HasConflictAsync(Guid resourceId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null);

    /// <summary>
    /// Gets all bookings for a specific resource (tenant-filtered).
    /// </summary>
    Task<IEnumerable<Booking>> GetByResourceIdAsync(Guid resourceId);

    /// <summary>
    /// Gets all bookings for a specific user (tenant-filtered).
    /// </summary>
    Task<IEnumerable<Booking>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets all bookings with a specific status (tenant-filtered).
    /// </summary>
    Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status);
    
    /// <summary>
    /// Gets paginated bookings with advanced filtering and sorting (tenant-filtered).
    /// </summary>
    Task<PagedResult<Booking>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? resourceId = null,
        Guid? userId = null,
        BookingStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string orderBy = "StartTime",
        bool descending = false,
        CancellationToken cancellationToken = default);
}
