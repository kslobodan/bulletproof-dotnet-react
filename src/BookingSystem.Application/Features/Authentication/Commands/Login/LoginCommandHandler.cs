using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Authentication.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Simple DTO for user login query
/// </summary>
internal class UserLoginDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Handler for LoginCommand.
/// Authenticates user by email/password and generates JWT token and refresh token.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        ITenantRepository tenantRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDbConnectionFactory connectionFactory,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _tenantRepository = tenantRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _connectionFactory = connectionFactory;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Find tenant by email
        var tenant = await _tenantRepository.GetByEmailAsync(request.Email);
        if (tenant == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // 2. Find user within tenant
        using var connection = _connectionFactory.CreateConnection();
        
        var userSql = @"
            SELECT Id, TenantId, Email, PasswordHash, FirstName, LastName, IsActive
            FROM Users
            WHERE Email = @Email AND TenantId = @TenantId";

        var user = await connection.QuerySingleOrDefaultAsync<UserLoginDto>(userSql, new
        {
            Email = request.Email,
            TenantId = tenant.Id
        });

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // 3. Check if user is active
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive");
        }

        // 4. Verify password
        var passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
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
            TenantId = tenant.Id
        })).ToList();

        // 6. Generate JWT token
        var token = _jwtTokenService.GenerateToken(user.Id, user.Email, roles);

        // 7. Generate and store refresh token
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = user.Id,
            TenantId = tenant.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days refresh token lifetime
            IsRevoked = false
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);

        // 8. Return response
        return new LoginResponse
        {
            AuthResult = new AuthResult
            {
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                TenantId = tenant.Id,
                TenantName = tenant.Name
            },
            Message = "Login successful"
        };
    }
}
