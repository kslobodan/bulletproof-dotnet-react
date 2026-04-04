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

        // TODO: Implement filtering in repository for better performance
        // For now, get all bookings and filter in memory
        var allBookings = await _bookingRepository.GetAllAsync(cancellationToken);

        // Apply filters
        var filteredBookings = allBookings.AsQueryable();

        if (request.ResourceId.HasValue)
        {
            filteredBookings = filteredBookings.Where(b => b.ResourceId == request.ResourceId.Value);
        }

        if (request.UserId.HasValue)
        {
            filteredBookings = filteredBookings.Where(b => b.UserId == request.UserId.Value);
        }

        if (request.Status.HasValue)
        {
            filteredBookings = filteredBookings.Where(b => b.Status == request.Status.Value);
        }

        if (request.StartDate.HasValue)
        {
            filteredBookings = filteredBookings.Where(b => b.StartTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            filteredBookings = filteredBookings.Where(b => b.EndTime <= request.EndDate.Value);
        }

        // Order by StartTime descending
        var orderedBookings = filteredBookings.OrderByDescending(b => b.StartTime).ToList();

        // Apply pagination
        var totalCount = orderedBookings.Count;
        var pagedBookings = orderedBookings
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var bookingDtos = _mapper.Map<List<BookingDto>>(pagedBookings);

        return new PagedResult<BookingDto>(
            bookingDtos,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
