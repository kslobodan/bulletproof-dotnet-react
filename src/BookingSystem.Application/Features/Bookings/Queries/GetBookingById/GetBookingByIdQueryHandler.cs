using AutoMapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Queries.GetBookingById;

public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;

    public GetBookingByIdQueryHandler(
        IBookingRepository bookingRepository,
        IMapper mapper,
        ITenantContext tenantContext)
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
        _tenantContext = tenantContext;
    }

    public async Task<BookingDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required for this operation");
        }

        var booking = await _bookingRepository.GetByIdAsync(request.Id);
        
        if (booking == null)
        {
            throw new KeyNotFoundException($"Booking with ID {request.Id} not found");
        }

        return _mapper.Map<BookingDto>(booking);
    }
}
