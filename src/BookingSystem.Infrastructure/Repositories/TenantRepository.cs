using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Models;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Infrastructure.Repositories;

/// <summary>
/// Tenant repository - NOT tenant-scoped since Tenants are global entities.
/// Each Tenant represents an organization in the multi-tenant system.
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TenantRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> AddAsync(Tenant entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO Tenants (Id, Name, Email, Plan, IsActive, CreatedAt)
            VALUES (@Id, @Name, @Email, @Plan, @IsActive, @CreatedAt)";

        await connection.ExecuteAsync(sql, entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Tenant entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        entity.UpdatedAt = DateTime.UtcNow;

        var sql = @"
            UPDATE Tenants
            SET Name = @Name,
                Email = @Email,
                Plan = @Plan,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, entity);

        return rowsAffected > 0;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = "SELECT * FROM Tenants WHERE Id = @Id";

        return await connection.QuerySingleOrDefaultAsync<Tenant>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = "SELECT * FROM Tenants";

        return await connection.QueryAsync<Tenant>(sql);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = "DELETE FROM Tenants WHERE Id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

        return rowsAffected > 0;
    }

    /// <summary>
    /// Soft delete is not supported for Tenants - they are critical infrastructure entities
    /// </summary>
    public Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Soft delete is not supported for Tenant entities. Use hard delete with caution.");
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = "SELECT COUNT(1)::int FROM Tenants WHERE Id = @Id";

        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });

        return count > 0;
    }

    public async Task<PagedResult<Tenant>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Get total count
        var countSql = "SELECT COUNT(*)::int FROM Tenants";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

        // Get paginated data
        var offset = (pageNumber - 1) * pageSize;
        var dataSql = @"
            SELECT * FROM Tenants 
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset";

        var items = await connection.QueryAsync<Tenant>(dataSql, new
        {
            PageSize = pageSize,
            Offset = offset
        });

        return new PagedResult<Tenant>(
            items.ToList(),
            totalCount,
            pageNumber,
            pageSize
        );
    }

    public async Task<Tenant?> GetByEmailAsync(string email)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = "SELECT * FROM Tenants WHERE Email = @Email";

        return await connection.QuerySingleOrDefaultAsync<Tenant>(sql, new { Email = email });
    }
}
