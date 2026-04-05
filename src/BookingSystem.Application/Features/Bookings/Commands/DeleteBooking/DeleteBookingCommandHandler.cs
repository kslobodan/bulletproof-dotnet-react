using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.DeleteBooking;

/// <summary>
/// Handler for DeleteBookingCommand
/// </summary>
public class DeleteBookingCommandHandler : IRequestHandler<DeleteBookingCommand, DeleteBookingResponse>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ITenantContext _tenantContext;

    public DeleteBookingCommandHandler(
        IBookingRepository bookingRepository,
        ITenantContext tenantContext)
    {
        _bookingRepository = bookingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<DeleteBookingResponse> Handle(DeleteBookingCommand request, CancellationToken cancellationToken)
    {
        // Validate tenant context
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        // Verify booking exists (repository filters by tenant automatically)
        var booking = await _bookingRepository.GetByIdAsync(request.Id);
        if (booking == null)
            throw new KeyNotFoundException($"Booking with ID {request.Id} not found");

        // Soft delete (marks as deleted, preserves data for audit trail)
        await _bookingRepository.SoftDeleteAsync(request.Id);

        return new DeleteBookingResponse
        {
            BookingId = request.Id,
            Message = "Booking deleted successfully"
        };
    }
}
