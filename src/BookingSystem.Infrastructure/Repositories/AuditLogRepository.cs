using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Models;
using BookingSystem.Domain.Entities;
using Dapper;
using System.Text;

namespace BookingSystem.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;

    public AuditLogRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
    }

    public async Task AddAsync(AuditLog auditLog)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        auditLog.Id = Guid.NewGuid();
        auditLog.TenantId = _tenantContext.TenantId;
        auditLog.Timestamp = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO AuditLogs (Id, TenantId, EntityName, EntityId, Action, OldValues, NewValues, UserId, Timestamp, IpAddress, Reason)
            VALUES (@Id, @TenantId, @EntityName, @EntityId, @Action, @OldValues, @NewValues, @UserId, @Timestamp, @IpAddress, @Reason)";

        await connection.ExecuteAsync(sql, auditLog);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid entityId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT * FROM AuditLogs
            WHERE TenantId = @TenantId 
              AND EntityName = @EntityName 
              AND EntityId = @EntityId
            ORDER BY Timestamp DESC";

        return await connection.QueryAsync<AuditLog>(sql, new
        {
            TenantId = _tenantContext.TenantId,
            EntityName = entityName,
            EntityId = entityId
        });
    }

    public async Task<PagedResult<AuditLog>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? entityName = null,
        string? action = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();

        var whereConditions = new List<string> { "TenantId = @TenantId" };
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", _tenantContext.TenantId);

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            whereConditions.Add("EntityName = @EntityName");
            parameters.Add("EntityName", entityName);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            whereConditions.Add("Action = @Action");
            parameters.Add("Action", action);
        }

        if (startDate.HasValue)
        {
            whereConditions.Add("Timestamp >= @StartDate");
            parameters.Add("StartDate", startDate.Value);
        }

        if (endDate.HasValue)
        {
            whereConditions.Add("Timestamp <= @EndDate");
            parameters.Add("EndDate", endDate.Value);
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // Get total count
        var countSql = $"SELECT COUNT(*) FROM AuditLogs WHERE {whereClause}";
        var totalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Get paged data
        var offset = (pageNumber - 1) * pageSize;
        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", offset);

        var dataSql = $@"
            SELECT * FROM AuditLogs
            WHERE {whereClause}
            ORDER BY Timestamp DESC
            LIMIT @Limit OFFSET @Offset";

        var items = await connection.QueryAsync<AuditLog>(dataSql, parameters);

        return new PagedResult<AuditLog>(items.ToList(), totalItems, pageNumber, pageSize);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId, int limit = 50)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT * FROM AuditLogs
            WHERE TenantId = @TenantId 
              AND UserId = @UserId
            ORDER BY Timestamp DESC
            LIMIT @Limit";

        return await connection.QueryAsync<AuditLog>(sql, new
        {
            TenantId = _tenantContext.TenantId,
            UserId = userId,
            Limit = limit
        });
    }
}
