using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Models;
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
}
