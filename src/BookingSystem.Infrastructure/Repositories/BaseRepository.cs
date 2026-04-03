using System.Data;
using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Extensions;
using BookingSystem.Application.Common.Models;

namespace BookingSystem.Infrastructure.Repositories;

/// <summary>
/// Base repository implementation with automatic tenant filtering.
/// All queries include TenantId in WHERE clauses for data isolation.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IDbConnectionFactory _connectionFactory;
    protected readonly ITenantContext _tenantContext;
    protected abstract string TableName { get; }
    protected abstract string IdColumnName { get; }

    protected BaseRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Gets the current tenant ID. Throws if tenant context is not resolved.
    /// </summary>
    protected Guid TenantId => _tenantContext.TenantId;

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var sql = $@"
            SELECT * FROM {TableName} 
            WHERE {IdColumnName} = @Id AND TenantId = @TenantId";
        
        return await connection.QuerySingleOrDefaultWithTenantAsync<T>(
            _tenantContext, sql, new { Id = id });
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var sql = $@"
            SELECT * FROM {TableName} 
            WHERE TenantId = @TenantId 
            ORDER BY CreatedAt DESC";
        
        return await connection.QueryWithTenantAsync<T>(_tenantContext, sql);
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Get total count
        var countSql = $@"
            SELECT COUNT(*)::int FROM {TableName} 
            WHERE TenantId = @TenantId";
        
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { TenantId = TenantId });
        
        // Get paginated data
        var offset = (pageNumber - 1) * pageSize;
        var dataSql = $@"
            SELECT * FROM {TableName} 
            WHERE TenantId = @TenantId 
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset";
        
        var items = await connection.QueryAsync<T>(dataSql, new 
        { 
            TenantId = TenantId,
            PageSize = pageSize,
            Offset = offset
        });
        
        return new PagedResult<T>(
            items.ToList(),
            totalCount,
            pageNumber,
            pageSize
        );
    }

    public abstract Task<Guid> AddAsync(T entity, CancellationToken cancellationToken = default);

    public abstract Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var sql = $@"
            DELETE FROM {TableName} 
            WHERE {IdColumnName} = @Id AND TenantId = @TenantId";
        
        var rowsAffected = await connection.ExecuteWithTenantAsync(
            _tenantContext, sql, new { Id = id });
        
        return rowsAffected > 0;
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var sql = $@"
            SELECT COUNT(1)::int FROM {TableName} 
            WHERE {IdColumnName} = @Id AND TenantId = @TenantId";
        
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id, TenantId = TenantId });
        
        return count > 0;
    }
}
