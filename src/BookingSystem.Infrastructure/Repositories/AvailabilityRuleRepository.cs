using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Models;
using BookingSystem.Domain.Entities;
using System.Text;

namespace BookingSystem.Infrastructure.Repositories;

/// <summary>
/// Repository for AvailabilityRule entity with tenant-scoped operations
/// </summary>
public class AvailabilityRuleRepository : BaseRepository<AvailabilityRule>, IAvailabilityRuleRepository
{
    public AvailabilityRuleRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory, tenantContext)
    {
    }

    protected override string TableName => "AvailabilityRules";
    protected override string IdColumnName => "Id";

    public override async Task<Guid> AddAsync(AvailabilityRule entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.TenantId = TenantId;

        var sql = @"
            INSERT INTO AvailabilityRules (
                Id, TenantId, ResourceId, DayOfWeek, 
                StartTime, EndTime, IsActive, 
                EffectiveFrom, EffectiveTo, CreatedAt
            )
            VALUES (
                @Id, @TenantId, @ResourceId, @DayOfWeek, 
                @StartTime, @EndTime, @IsActive, 
                @EffectiveFrom, @EffectiveTo, @CreatedAt
            )";

        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.TenantId,
            entity.ResourceId,
            DayOfWeek = (int)entity.DayOfWeek,
            entity.StartTime,
            entity.EndTime,
            entity.IsActive,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            entity.CreatedAt
        });

        return entity.Id;
    }

    public override async Task<bool> UpdateAsync(AvailabilityRule entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        entity.UpdatedAt = DateTime.UtcNow;

        var sql = @"
            UPDATE AvailabilityRules
            SET 
                StartTime = @StartTime,
                EndTime = @EndTime,
                IsActive = @IsActive,
                EffectiveFrom = @EffectiveFrom,
                EffectiveTo = @EffectiveTo,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND TenantId = @TenantId";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.StartTime,
            entity.EndTime,
            entity.IsActive,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            entity.UpdatedAt,
            TenantId = TenantId
        });

        return rowsAffected > 0;
    }

    public async Task<IEnumerable<AvailabilityRule>> GetByResourceIdAsync(Guid resourceId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = @"
            SELECT * FROM AvailabilityRules 
            WHERE TenantId = @TenantId 
              AND ResourceId = @ResourceId
              AND IsDeleted = FALSE
            ORDER BY DayOfWeek, StartTime";

        return await connection.QueryAsync<AvailabilityRule>(sql, new
        {
            TenantId = TenantId,
            ResourceId = resourceId
        });
    }

    public async Task<PagedResult<AvailabilityRule>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? resourceId = null,
        DayOfWeek? dayOfWeek = null,
        bool? isActive = null)
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

        if (dayOfWeek.HasValue)
        {
            whereConditions.Add("DayOfWeek = @DayOfWeek");
            parameters.Add("DayOfWeek", (int)dayOfWeek.Value);
        }

        if (isActive.HasValue)
        {
            whereConditions.Add("IsActive = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // Get total count
        var countSql = $@"
            SELECT COUNT(*)::int FROM AvailabilityRules 
            WHERE {whereClause}";

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Get paginated data
        var offset = (pageNumber - 1) * pageSize;
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        var dataSql = $@"
            SELECT * FROM AvailabilityRules 
            WHERE {whereClause}
            ORDER BY DayOfWeek, StartTime
            LIMIT @PageSize OFFSET @Offset";

        var items = await connection.QueryAsync<AvailabilityRule>(dataSql, parameters);

        return new PagedResult<AvailabilityRule>(
            items.ToList(),
            totalCount,
            pageNumber,
            pageSize
        );
    }
}
