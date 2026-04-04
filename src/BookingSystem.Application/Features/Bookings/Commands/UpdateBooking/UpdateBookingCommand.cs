using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.UpdateBooking;

/// <summary>
/// Command to update an existing booking
/// </summary>
public class UpdateBookingCommand : IRequest<UpdateBookingResponse>
{
    public Guid Id { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
}
