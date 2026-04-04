namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// Response after creating a booking
/// </summary>
public class CreateBookingResponse
{
    public BookingDto Booking { get; set; } = null!;
    public string Message { get; set; } = "Booking created successfully";
}
