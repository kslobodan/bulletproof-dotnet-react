namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// Request for creating a new booking
/// </summary>
public class CreateBookingRequest
{
    public Guid ResourceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}
