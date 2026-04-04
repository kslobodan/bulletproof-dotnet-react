using BookingSystem.Application.Common.Models;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Enums;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Queries.GetAllBookings;

public class GetAllBookingsQuery : IRequest<PagedResult<BookingDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // Optional filters
    public Guid? ResourceId { get; set; }
    public Guid? UserId { get; set; }
    public BookingStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
