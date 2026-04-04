namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// Response after cancelling a booking
/// </summary>
public class CancelBookingResponse
{
    public Guid BookingId { get; set; }
    public string Message { get; set; } = "Booking cancelled successfully";
}
