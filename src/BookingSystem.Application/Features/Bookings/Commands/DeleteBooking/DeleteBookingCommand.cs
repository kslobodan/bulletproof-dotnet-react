using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.DeleteBooking;

/// <summary>
/// Command to delete a booking (admin only)
/// </summary>
public class DeleteBookingCommand : IRequest<DeleteBookingResponse>
{
    public Guid Id { get; set; }
}
