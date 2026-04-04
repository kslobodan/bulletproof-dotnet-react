namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// Request for updating an existing booking
/// </summary>
public class UpdateBookingRequest
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
}
