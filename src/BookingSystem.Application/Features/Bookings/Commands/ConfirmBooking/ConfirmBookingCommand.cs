using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.ConfirmBooking;

/// <summary>
/// Command to confirm a booking (admin/manager action)
/// </summary>
public class ConfirmBookingCommand : IRequest<ConfirmBookingResponse>
{
    public Guid Id { get; set; }
}
