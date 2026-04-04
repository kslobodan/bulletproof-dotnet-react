using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Enums;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.UpdateBooking;

/// <summary>
/// Handler for UpdateBookingCommand
/// </summary>
public class UpdateBookingCommandHandler : IRequestHandler<UpdateBookingCommand, UpdateBookingResponse>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateBookingCommandHandler(
        IBookingRepository bookingRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _bookingRepository = bookingRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateBookingResponse> Handle(UpdateBookingCommand request, CancellationToken cancellationToken)
    {
        // Validate tenant context
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        // Get existing booking (repository filters by tenant automatically)
        var booking = await _bookingRepository.GetByIdAsync(request.Id);
        if (booking == null)
            throw new KeyNotFoundException($"Booking with ID {request.Id} not found");

        // Only allow updates to pending bookings
        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot update booking with status {booking.Status}");

        // Update time range if provided
        if (request.StartTime.HasValue || request.EndTime.HasValue)
        {
            var newStartTime = request.StartTime ?? booking.StartTime;
            var newEndTime = request.EndTime ?? booking.EndTime;

            // Check for conflicts with new time range
            var hasConflict = await _bookingRepository.HasConflictAsync(
                booking.ResourceId,
                newStartTime,
                newEndTime,
                excludeBookingId: booking.Id);

            if (hasConflict)
                throw new InvalidOperationException("The new time slot conflicts with another booking");

            booking.StartTime = newStartTime;
            booking.EndTime = newEndTime;
        }

        // Update other fields if provided
        if (request.Title != null)
            booking.Title = request.Title;

        if (request.Description != null)
            booking.Description = request.Description;

        if (request.Notes != null)
            booking.Notes = request.Notes;

        booking.UpdatedAt = DateTime.UtcNow;
        booking.UpdatedBy = _currentUserService.UserId;

        // Save changes
        await _bookingRepository.UpdateAsync(booking);

        // Map to DTO
        var bookingDto = new BookingDto
        {
            Id = booking.Id,
            TenantId = booking.TenantId,
            ResourceId = booking.ResourceId,
            UserId = booking.UserId,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            Title = booking.Title,
            Description = booking.Description,
            Notes = booking.Notes,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
        };

        return new UpdateBookingResponse
        {
            Booking = bookingDto,
            Message = "Booking updated successfully"
        };
    }
}
