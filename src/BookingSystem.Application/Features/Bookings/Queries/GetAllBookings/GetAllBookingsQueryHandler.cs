using AutoMapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Models;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Entities;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Queries.GetAllBookings;

public class GetAllBookingsQueryHandler : IRequestHandler<GetAllBookingsQuery, PagedResult<BookingDto>>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;

    public GetAllBookingsQueryHandler(
        IBookingRepository bookingRepository,
        IMapper mapper,
        ITenantContext tenantContext)
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
        _tenantContext = tenantContext;
    }

    public async Task<PagedResult<BookingDto>> Handle(GetAllBookingsQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required for this operation");
        }

        // Use repository method with database-level filtering for better performance
        var pagedBookings = await _bookingRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.ResourceId,
            request.UserId,
            request.Status,
            request.StartDate,
            request.EndDate,
            request.OrderBy ?? "StartTime",
            request.Descending,
            cancellationToken
        );

        var bookingDtos = _mapper.Map<List<BookingDto>>(pagedBookings.Items);

        return new PagedResult<BookingDto>(
            bookingDtos,
            pagedBookings.TotalCount,
            pagedBookings.PageNumber,
            pagedBookings.PageSize
        );
    }
}
