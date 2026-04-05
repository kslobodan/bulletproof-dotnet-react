using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Queries.GetBookingStatistics;

/// <summary>
/// Query to get booking statistics for the current tenant
/// </summary>
public class GetBookingStatisticsQuery : IRequest<BookingStatisticsDto>
{
    /// <summary>
    /// Start date for statistics calculation (optional)
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// End date for statistics calculation (optional)
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Number of top resources to include in utilization stats (default: 10)
    /// </summary>
    public int TopResourcesCount { get; set; } = 10;
}
