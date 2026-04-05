using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Authentication.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshAccessTokenCommand.
/// Validates refresh token, generates new access token and refresh token (token rotation).
/// </summary>
public class RefreshAccessTokenCommandHandler : IRequestHandler<RefreshAccessTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshAccessTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IDbConnectionFactory connectionFactory)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _connectionFactory = connectionFactory;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate and retrieve refresh token
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
        if (refreshToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // 2. Check if token is revoked
        if (refreshToken.IsRevoked)
        {
            throw new UnauthorizedAccessException("Refresh token has been revoked");
        }

        // 3. Check if token is expired
        if (refreshToken.IsExpired)
        {
            throw new UnauthorizedAccessException("Refresh token has expired");
        }

        // 4. Get user information
        using var connection = _connectionFactory.CreateConnection();
        
        var userSql = @"
            SELECT Id, TenantId, Email, FirstName, LastName, IsActive
            FROM Users
            WHERE Id = @UserId AND TenantId = @TenantId";

        var user = await connection.QuerySingleOrDefaultAsync<UserInfoDto>(userSql, new
        {
            UserId = refreshToken.UserId,
            TenantId = refreshToken.TenantId
        });

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive");
        }

        // 5. Get user roles
        var rolesSql = @"
            SELECT r.Name
            FROM UserRoles ur
            INNER JOIN Roles r ON ur.RoleId = r.Id
            WHERE ur.UserId = @UserId AND ur.TenantId = @TenantId";

        var roles = (await connection.QueryAsync<string>(rolesSql, new
        {
            UserId = user.Id,
            TenantId = user.TenantId
        })).ToList();

        // 6. Get tenant information
        var tenantSql = "SELECT Id, Name FROM Tenants WHERE Id = @TenantId";
        var tenant = await connection.QuerySingleAsync<TenantInfoDto>(tenantSql, new { TenantId = user.TenantId });

        // 7. Generate new access token
        var newAccessToken = _jwtTokenService.GenerateToken(user.Id, user.Email, roles);

        // 8. Generate new refresh token
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            UserId = user.Id,
            TenantId = user.TenantId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days refresh token lifetime
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

        // 9. Revoke old refresh token (token rotation)
        await _refreshTokenRepository.RevokeAsync(refreshToken.Id, newRefreshToken);

        // 10. Return response with new tokens
        return new RefreshTokenResponse
        {
            AuthResult = new AuthResult
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                TenantId = user.TenantId,
                TenantName = tenant.Name
            },
            Message = "Token refreshed successfully"
        };
    }
}

/// <summary>
/// DTO for user information query
/// </summary>
internal class UserInfoDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for tenant information query
/// </summary>
internal class TenantInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
