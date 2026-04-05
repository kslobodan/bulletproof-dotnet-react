using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Common.Interfaces;

/// <summary>
/// Repository interface for RefreshToken operations.
/// Handles token storage, retrieval, and revocation.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Stores a new refresh token in the database.
    /// </summary>
    Task<RefreshToken> AddAsync(RefreshToken refreshToken);

    /// <summary>
    /// Retrieves a refresh token by its token value.
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// Revokes a refresh token (marks as revoked with timestamp).
    /// Optionally sets ReplacedByToken for token rotation tracking.
    /// </summary>
    Task RevokeAsync(Guid tokenId, string? replacedByToken = null);

    /// <summary>
    /// Deletes all expired and revoked refresh tokens for cleanup.
    /// Should be called periodically to prevent database bloat.
    /// </summary>
    Task DeleteExpiredAsync();
}
