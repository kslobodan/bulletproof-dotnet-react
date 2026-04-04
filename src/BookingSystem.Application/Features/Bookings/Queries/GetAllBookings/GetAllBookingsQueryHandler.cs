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

        // Build filter conditions
        string whereClause = "1=1"; // Start with always-true condition
        var parameters = new Dictionary<string, object>();

        if (request.ResourceId.HasValue)
        {
            whereClause += " AND ResourceId = @ResourceId";
            parameters.Add("ResourceId", request.ResourceId.Value);
        }

        if (request.UserId.HasValue)
        {
            whereClause += " AND UserId = @UserId";
            parameters.Add("UserId", request.UserId.Value);
        }

        if (request.Status.HasValue)
        {
            whereClause += " AND Status = @Status";
            parameters.Add("Status", (int)request.Status.Value);
        }

        if (request.StartDate.HasValue)
        {
            whereClause += " AND StartTime >= @StartDate";
            parameters.Add("StartDate", request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            whereClause += " AND EndTime <= @EndDate";
            parameters.Add("EndDate", request.EndDate.Value);
        }

        var pagedBookings = await _bookingRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            whereClause,
            parameters,
            "StartTime DESC"
        );

        var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(pagedBookings.Items);

        return new PagedResult<BookingDto>(
            bookingDtos,
            pagedBookings.TotalCount,
            pagedBookings.PageNumber,
            pagedBookings.PageSize
        );
    }
}
