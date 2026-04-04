namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// Response after confirming a booking
/// </summary>
public class ConfirmBookingResponse
{
    public Guid BookingId { get; set; }
    public string Message { get; set; } = "Booking confirmed successfully";
}
