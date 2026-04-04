using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Queries.GetBookingById;

public class GetBookingByIdQuery : IRequest<BookingDto>
{
    public Guid Id { get; set; }
}
