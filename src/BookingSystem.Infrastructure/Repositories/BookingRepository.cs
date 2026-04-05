using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Models;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Infrastructure.Repositories;

/// <summary>
/// Repository for Booking entity with tenant-scoped operations and conflict detection
/// </summary>
public class BookingRepository : BaseRepository<Booking>, IBookingRepository
{
    public BookingRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory, tenantContext)
    {
    }

    protected override string TableName => "Bookings";
    protected override string IdColumnName => "Id";

    public override async Task<Guid> AddAsync(Booking entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.TenantId = TenantId;

        var sql = @"
            INSERT INTO Bookings (
                Id, TenantId, ResourceId, UserId, 
                StartTime, EndTime, Status, 
                Title, Description, Notes, 
                CreatedAt, CreatedBy
            )
            VALUES (
                @Id, @TenantId, @ResourceId, @UserId, 
                @StartTime, @EndTime, @Status, 
                @Title, @Description, @Notes, 
                @CreatedAt, @CreatedBy
            )";

        await connection.ExecuteAsync(sql, entity);

        return entity.Id;
    }

    public override async Task<bool> UpdateAsync(Booking entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        entity.UpdatedAt = DateTime.UtcNow;

        var sql = @"
            UPDATE Bookings
            SET 
                ResourceId = @ResourceId,
                UserId = @UserId,
                StartTime = @StartTime,
                EndTime = @EndTime,
                Status = @Status,
                Title = @Title,
                Description = @Description,
                Notes = @Notes,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND TenantId = @TenantId";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.ResourceId,
            entity.UserId,
            entity.StartTime,
            entity.EndTime,
            Status = (int)entity.Status,
            entity.Title,
            entity.Description,
            entity.Notes,
            entity.UpdatedAt,
            entity.UpdatedBy,
            TenantId = TenantId
        });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Detects booking conflicts using time overlap logic.
    /// Two time ranges overlap if: (StartA < EndB) AND (EndA > StartB)
    /// Only considers Pending (0) and Confirmed (1) bookings as potential conflicts.
    /// Excludes soft-deleted bookings from conflict detection.
    /// </summary>
    public async Task<bool> HasConflictAsync(
        Guid resourceId, 
        DateTime startTime, 
        DateTime endTime, 
        Guid? excludeBookingId = null)
    {
        using var connection = _connectionFactory.CreateConnection();

        // SQL to detect overlapping bookings
        // Overlap condition: booking starts before new ends AND booking ends after new starts
        var sql = @"
            SELECT EXISTS (
                SELECT 1 
                FROM Bookings 
                WHERE TenantId = @TenantId
                  AND ResourceId = @ResourceId
                  AND Status IN (0, 1)  -- Pending or Confirmed only
                  AND IsDeleted = FALSE
                  AND Id != COALESCE(@ExcludeBookingId, '00000000-0000-0000-0000-000000000000'::uuid)
                  AND StartTime < @EndTime
                  AND EndTime > @StartTime
            )";

        var hasConflict = await connection.ExecuteScalarAsync<bool>(sql, new
        {
            TenantId = TenantId,
            ResourceId = resourceId,
            StartTime = startTime,
            EndTime = endTime,
            ExcludeBookingId = excludeBookingId
        });

        return hasConflict;
    }

    public async Task<IEnumerable<Booking>> GetByResourceIdAsync(Guid resourceId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT * FROM Bookings 
            WHERE TenantId = @TenantId 
              AND ResourceId = @ResourceId
              AND IsDeleted = FALSE
            ORDER BY StartTime DESC";

        return await connection.QueryAsync<Booking>(sql, new
        {
            TenantId = TenantId,
            ResourceId = resourceId
        });
    }

    public async Task<IEnumerable<Booking>> GetByUserIdAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT * FROM Bookings 
            WHERE TenantId = @TenantId 
              AND UserId = @UserId
              AND IsDeleted = FALSE
            ORDER BY StartTime DESC";

        return await connection.QueryAsync<Booking>(sql, new
        {
            TenantId = TenantId,
            UserId = userId
        });
    }

    public async Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT * FROM Bookings 
            WHERE TenantId = @TenantId 
              AND Status = @Status
              AND IsDeleted = FALSE
            ORDER BY StartTime DESC";

        return await connection.QueryAsync<Booking>(sql, new
        {
            TenantId = TenantId,
            Status = (int)status
        });
    }

    public async Task<PagedResult<Booking>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? resourceId = null,
        Guid? userId = null,
        BookingStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string orderBy = "StartTime",
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Build dynamic WHERE clause
        var whereConditions = new List<string> { "TenantId = @TenantId", "IsDeleted = FALSE" };
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", TenantId);

        if (resourceId.HasValue)
        {
            whereConditions.Add("ResourceId = @ResourceId");
            parameters.Add("ResourceId", resourceId.Value);
        }

        if (userId.HasValue)
        {
            whereConditions.Add("UserId = @UserId");
            parameters.Add("UserId", userId.Value);
        }

        if (status.HasValue)
        {
            whereConditions.Add("Status = @Status");
            parameters.Add("Status", (int)status.Value);
        }

        if (startDate.HasValue)
        {
            whereConditions.Add("StartTime >= @StartDate");
            parameters.Add("StartDate", startDate.Value);
        }

        if (endDate.HasValue)
        {
            whereConditions.Add("EndTime <= @EndDate");
            parameters.Add("EndDate", endDate.Value);
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // Validate and sanitize ORDER BY to prevent SQL injection
        var validOrderByColumns = new[] { "StartTime", "EndTime", "CreatedAt", "Status", "Title" };
        var sanitizedOrderBy = validOrderByColumns.Contains(orderBy) ? orderBy : "StartTime";
        var orderDirection = descending ? "DESC" : "ASC";

        // Get total count
        var countSql = $@"
            SELECT COUNT(*)::int FROM Bookings 
            WHERE {whereClause}";

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Get paginated data
        var offset = (pageNumber - 1) * pageSize;
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var dataSql = $@"
            SELECT * FROM Bookings 
            WHERE {whereClause}
            ORDER BY {sanitizedOrderBy} {orderDirection}
            LIMIT @PageSize OFFSET @Offset";

        var items = await connection.QueryAsync<Booking>(dataSql, parameters);

        return new PagedResult<Booking>(
            items.ToList(),
            totalCount,
            pageNumber,
            pageSize
        );
    }

    public async Task<BookingStatisticsDto> GetStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int topResourcesCount = 10,
        CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Build date filter
        var dateFilter = string.Empty;
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", TenantId);

        if (startDate.HasValue)
        {
            dateFilter += " AND StartTime >= @StartDate";
            parameters.Add("StartDate", startDate.Value);
        }

        if (endDate.HasValue)
        {
            dateFilter += " AND EndTime <= @EndDate";
            parameters.Add("EndDate", endDate.Value);
        }

        // 1. Get booking counts by status
        var statusCountsSql = $@"
            SELECT 
                Status,
                COUNT(*)::int as Count
            FROM Bookings
            WHERE TenantId = @TenantId
              AND IsDeleted = FALSE
              {dateFilter}
            GROUP BY Status";

        var statusCounts = await connection.QueryAsync<(int Status, int Count)>(statusCountsSql, parameters);
        var statusDict = statusCounts.ToDictionary(x => (BookingStatus)x.Status, x => x.Count);

        var totalBookings = statusDict.Values.Sum();
        var pendingCount = statusDict.GetValueOrDefault(BookingStatus.Pending, 0);
        var confirmedCount = statusDict.GetValueOrDefault(BookingStatus.Confirmed, 0);
        var completedCount = statusDict.GetValueOrDefault(BookingStatus.Completed, 0);
        var cancelledCount = statusDict.GetValueOrDefault(BookingStatus.Cancelled, 0);

        // Calculate rates
        var confirmationRate = totalBookings > 0 
            ? (decimal)(confirmedCount + completedCount) / totalBookings * 100 
            : 0;
        var cancellationRate = totalBookings > 0 
            ? (decimal)cancelledCount / totalBookings * 100 
            : 0;

        // 2. Get resource utilization (top N resources by booking count)
        parameters.Add("TopCount", topResourcesCount);
        var resourceUtilizationSql = $@"
            SELECT 
                r.Id as ResourceId,
                r.Name as ResourceName,
                COUNT(b.Id)::int as TotalBookings,
                ROUND(
                    (COUNT(b.Id)::decimal / NULLIF(
                        (SELECT COUNT(*)::decimal 
                         FROM Bookings 
                         WHERE TenantId = @TenantId 
                           AND IsDeleted = FALSE
                           {dateFilter}), 0
                    ) * 100), 2
                ) as UtilizationPercentage
            FROM Resources r
            LEFT JOIN Bookings b ON r.Id = b.ResourceId 
                AND b.TenantId = @TenantId 
                AND b.IsDeleted = FALSE
                {dateFilter.Replace("StartTime", "b.StartTime").Replace("EndTime", "b.EndTime")}
            WHERE r.TenantId = @TenantId
              AND r.IsDeleted = FALSE
            GROUP BY r.Id, r.Name
            ORDER BY TotalBookings DESC
            LIMIT @TopCount";

        var resourceUtilization = await connection.QueryAsync<ResourceUtilizationDto>(resourceUtilizationSql, parameters);

        // 3. Get popular time slots (group by hour of day)
        var popularTimeSlotsSql = $@"
            SELECT 
                EXTRACT(HOUR FROM StartTime)::int as HourOfDay,
                COUNT(*)::int as BookingCount,
                TO_CHAR(EXTRACT(HOUR FROM StartTime)::int, 'FM00') || ':00-' || 
                TO_CHAR((EXTRACT(HOUR FROM StartTime)::int + 1), 'FM00') || ':00' as TimeSlotLabel
            FROM Bookings
            WHERE TenantId = @TenantId
              AND IsDeleted = FALSE
              AND Status IN (0, 1, 2)  -- Pending, Confirmed, Completed
              {dateFilter}
            GROUP BY HourOfDay
            ORDER BY BookingCount DESC
            LIMIT 10";

        var popularTimeSlots = await connection.QueryAsync<PopularTimeSlotDto>(popularTimeSlotsSql, parameters);

        return new BookingStatisticsDto
        {
            TotalBookings = totalBookings,
            PendingBookings = pendingCount,
            ConfirmedBookings = confirmedCount,
            CompletedBookings = completedCount,
            CancelledBookings = cancelledCount,
            ConfirmationRate = Math.Round(confirmationRate, 2),
            CancellationRate = Math.Round(cancellationRate, 2),
            ResourceUtilization = resourceUtilization.ToList(),
            PopularTimeSlots = popularTimeSlots.ToList()
        };
    }
}
