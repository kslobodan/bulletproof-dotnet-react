namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// Response after deleting a booking
/// </summary>
public class DeleteBookingResponse
{
    public Guid BookingId { get; set; }
    public string Message { get; set; } = "Booking deleted successfully";
}
