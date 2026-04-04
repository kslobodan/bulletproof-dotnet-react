using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Command to create a new booking
/// </summary>
public class CreateBookingCommand : IRequest<CreateBookingResponse>
{
    public Guid ResourceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}
