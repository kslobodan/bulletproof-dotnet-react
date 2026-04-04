using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.CancelBooking;

/// <summary>
/// Command to cancel a booking
/// </summary>
public class CancelBookingCommand : IRequest<CancelBookingResponse>
{
    public Guid Id { get; set; }
}
