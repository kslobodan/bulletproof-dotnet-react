using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Infrastructure.Repositories;

/// <summary>
/// Repository for Resource entity with tenant-scoped operations
/// </summary>
public class ResourceRepository : BaseRepository<Resource>, IResourceRepository
{
    public ResourceRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory, tenantContext)
    {
    }

    protected override string TableName => "Resources";
    protected override string IdColumnName => "Id";

    public override async Task<Guid> AddAsync(Resource entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.TenantId = TenantId;

        var sql = @"
            INSERT INTO Resources (Id, Name, Description, ResourceType, Capacity, IsActive, TenantId, CreatedAt)
            VALUES (@Id, @Name, @Description, @ResourceType, @Capacity, @IsActive, @TenantId, @CreatedAt)";

        await connection.ExecuteAsync(sql, entity);

        return entity.Id;
    }

    public override async Task<bool> UpdateAsync(Resource entity, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        entity.UpdatedAt = DateTime.UtcNow;

        var sql = @"
            UPDATE Resources
            SET Name = @Name,
                Description = @Description,
                ResourceType = @ResourceType,
                Capacity = @Capacity,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND TenantId = @TenantId";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            entity.Name,
            entity.Description,
            entity.ResourceType,
            entity.Capacity,
            entity.IsActive,
            entity.UpdatedAt,
            TenantId = TenantId
        });

        return rowsAffected > 0;
    }
}
