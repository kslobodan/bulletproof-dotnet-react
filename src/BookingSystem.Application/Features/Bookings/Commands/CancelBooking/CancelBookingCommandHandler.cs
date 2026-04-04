using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Enums;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.CancelBooking;

/// <summary>
/// Handler for CancelBookingCommand
/// </summary>
public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, CancelBookingResponse>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ITenantContext _tenantContext;

    public CancelBookingCommandHandler(
        IBookingRepository bookingRepository,
        ITenantContext tenantContext)
    {
        _bookingRepository = bookingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<CancelBookingResponse> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        // Validate tenant context
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        // Get booking (repository filters by tenant automatically)
        var booking = await _bookingRepository.GetByIdAsync(request.Id);
        if (booking == null)
            throw new KeyNotFoundException($"Booking with ID {request.Id} not found");

        // Only allow cancellation of pending or confirmed bookings
        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Cannot cancel booking with status {booking.Status}");

        // Update status to cancelled
        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedBy = _tenantContext.TenantId; // TODO: Get actual UserId

        await _bookingRepository.UpdateAsync(booking);

        return new CancelBookingResponse
        {
            BookingId = booking.Id,
            Message = "Booking cancelled successfully"
        };
    }
}
