using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Infrastructure.Repositories;

/// <summary>
/// Repository for RefreshToken operations using Dapper.
/// Handles token storage, retrieval, revocation, and cleanup.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RefreshToken> AddAsync(RefreshToken refreshToken)
    {
        var sql = @"
            INSERT INTO RefreshTokens (Id, Token, UserId, TenantId, CreatedAt, ExpiresAt, IsRevoked, RevokedAt, ReplacedByToken)
            VALUES (@Id, @Token, @UserId, @TenantId, @CreatedAt, @ExpiresAt, @IsRevoked, @RevokedAt, @ReplacedByToken)
            RETURNING *";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QuerySingleAsync<RefreshToken>(sql, refreshToken);
        return result;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        var sql = @"
            SELECT Id, Token, UserId, TenantId, CreatedAt, ExpiresAt, IsRevoked, RevokedAt, ReplacedByToken
            FROM RefreshTokens
            WHERE Token = @Token";

        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { Token = token });
        return result;
    }

    public async Task RevokeAsync(Guid tokenId, string? replacedByToken = null)
    {
        var sql = @"
            UPDATE RefreshTokens
            SET IsRevoked = true,
                RevokedAt = @RevokedAt,
                ReplacedByToken = @ReplacedByToken
            WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = tokenId,
            RevokedAt = DateTime.UtcNow,
            ReplacedByToken = replacedByToken
        });
    }

    public async Task DeleteExpiredAsync()
    {
        var sql = @"
            DELETE FROM RefreshTokens
            WHERE (IsRevoked = true OR ExpiresAt < @Now)
            AND CreatedAt < @CutoffDate";

        using var connection = _connectionFactory.CreateConnection();
        
        // Delete tokens that are:
        // 1. Revoked OR expired
        // 2. AND older than 30 days (keep recent tokens for audit trail)
        await connection.ExecuteAsync(sql, new
        {
            Now = DateTime.UtcNow,
            CutoffDate = DateTime.UtcNow.AddDays(-30)
        });
    }
}
