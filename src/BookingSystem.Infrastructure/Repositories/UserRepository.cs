using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Common.Extensions;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Infrastructure.Repositories;

/// <summary>
/// User repository with automatic tenant filtering.
/// Demonstrates multi-tenant data isolation pattern.
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    protected override string TableName => "Users";
    protected override string IdColumnName => "Id";

    public UserRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory, tenantContext)
    {
    }

    public override async Task<Guid> AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        entity.Id = Guid.NewGuid();
        entity.TenantId = TenantId; // Automatically set from context
        entity.CreatedAt = DateTime.UtcNow;
        
        var sql = @"
            INSERT INTO Users (Id, TenantId, Email, PasswordHash, FirstName, LastName, IsActive, CreatedAt)
            VALUES (@Id, @TenantId, @Email, @PasswordHash, @FirstName, @LastName, @IsActive, @CreatedAt)";
        
        await connection.ExecuteAsync(sql, entity);
        
        return entity.Id;
    }

    public override async Task<bool> UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        entity.UpdatedAt = DateTime.UtcNow;
        
        var sql = @"
            UPDATE Users 
            SET Email = @Email,
                PasswordHash = @PasswordHash,
                FirstName = @FirstName,
                LastName = @LastName,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND TenantId = @TenantId";
        
        var rowsAffected = await connection.ExecuteWithTenantAsync(
            _tenantContext, sql, entity);
        
        return rowsAffected > 0;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var sql = @"
            SELECT * FROM Users 
            WHERE Email = @Email AND TenantId = @TenantId";
        
        return await connection.QuerySingleOrDefaultWithTenantAsync<User>(
            _tenantContext, sql, new { Email = email });
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        var sql = @"
            SELECT COUNT(1)::int FROM Users 
            WHERE Email = @Email AND TenantId = @TenantId";
        
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, TenantId = TenantId });
        
        return count > 0;
    }
}
