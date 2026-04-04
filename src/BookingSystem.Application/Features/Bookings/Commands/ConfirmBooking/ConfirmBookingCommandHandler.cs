using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Enums;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.ConfirmBooking;

/// <summary>
/// Handler for ConfirmBookingCommand
/// </summary>
public class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand, ConfirmBookingResponse>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ITenantContext _tenantContext;

    public ConfirmBookingCommandHandler(
        IBookingRepository bookingRepository,
        ITenantContext tenantContext)
    {
        _bookingRepository = bookingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ConfirmBookingResponse> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        // Validate tenant context
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        // Get booking (repository filters by tenant automatically)
        var booking = await _bookingRepository.GetByIdAsync(request.Id);
        if (booking == null)
            throw new KeyNotFoundException($"Booking with ID {request.Id} not found");

        // Only allow confirmation of pending bookings
        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm booking with status {booking.Status}");

        // Update status to confirmed
        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedBy = _tenantContext.TenantId; // TODO: Get actual UserId

        await _bookingRepository.UpdateAsync(booking);

        return new ConfirmBookingResponse
        {
            BookingId = booking.Id,
            Message = "Booking confirmed successfully"
        };
    }
}
