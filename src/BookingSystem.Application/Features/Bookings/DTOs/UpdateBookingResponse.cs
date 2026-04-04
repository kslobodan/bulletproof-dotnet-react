namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// Response after updating a booking
/// </summary>
public class UpdateBookingResponse
{
    public BookingDto Booking { get; set; } = null!;
    public string Message { get; set; } = "Booking updated successfully";
}
