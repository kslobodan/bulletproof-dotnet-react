using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Bookings.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Bookings.Queries.GetBookingStatistics;

public class GetBookingStatisticsQueryHandler : IRequestHandler<GetBookingStatisticsQuery, BookingStatisticsDto>
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ITenantContext _tenantContext;

    public GetBookingStatisticsQueryHandler(
        IBookingRepository bookingRepository,
        ITenantContext tenantContext)
    {
        _bookingRepository = bookingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<BookingStatisticsDto> Handle(GetBookingStatisticsQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
        {
            throw new UnauthorizedAccessException("Tenant context is required for this operation");
        }

        var statistics = await _bookingRepository.GetStatisticsAsync(
            request.StartDate,
            request.EndDate,
            request.TopResourcesCount,
            cancellationToken);

        return statistics;
    }
}
