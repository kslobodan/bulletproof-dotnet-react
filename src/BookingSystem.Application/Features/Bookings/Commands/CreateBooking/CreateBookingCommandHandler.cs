using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Handler for CreateBookingCommand
/// </summary>
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly ITenantContext _tenantContext;

    public CreateBookingCommandHandler(
        IBookingRepository bookingRepository,
        IResourceRepository resourceRepository,
        ITenantContext tenantContext)
    {
        _bookingRepository = bookingRepository;
        _resourceRepository = resourceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // Validate tenant context
        if (!_tenantContext.IsResolved)
            throw new UnauthorizedAccessException("Tenant context is required");

        var tenantId = _tenantContext.TenantId;

        // Verify resource exists and is active
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId);
        if (resource == null)
            throw new KeyNotFoundException($"Resource with ID {request.ResourceId} not found");

        if (!resource.IsActive)
            throw new InvalidOperationException("Cannot book an inactive resource");

        // Check for booking conflicts (will implement in repository)
        var hasConflict = await _bookingRepository.HasConflictAsync(
            request.ResourceId, 
            request.StartTime, 
            request.EndTime,
            excludeBookingId: null);

        if (hasConflict)
            throw new InvalidOperationException("This time slot is already booked");

        // Create booking entity
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ResourceId = request.ResourceId,
            UserId = tenantId, // TODO: Get actual UserId from authentication context
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = BookingStatus.Pending,
            Title = request.Title,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenantId // TODO: Get actual UserId
        };

        // Save to database
        await _bookingRepository.AddAsync(booking);

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

        return new CreateBookingResponse
        {
            Booking = bookingDto,
            Message = "Booking created successfully"
        };
    }
}
