using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Authentication.DTOs;
using BookingSystem.Domain.Entities;
using MediatR;

namespace BookingSystem.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// Handler for RegisterUserCommand.
/// Creates a new user within the current tenant context.
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITenantContext _tenantContext;
    private readonly IDbConnectionFactory _connectionFactory;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ITenantContext tenantContext,
        IDbConnectionFactory connectionFactory)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _tenantContext = tenantContext;
        _connectionFactory = connectionFactory;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Ensure tenant context is set
        if (!_tenantContext.IsResolved)
        {
            throw new InvalidOperationException("Tenant context is not resolved. X-Tenant-Id header is required.");
        }

        var tenantId = _tenantContext.TenantId;

        // 2. Check if user email already exists in this tenant
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists in this tenant");
        }

        // 3. Get tenant details
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException("Tenant not found");
        }

        // 4. Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 5. Create user
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true
        };

        var userId = await _userRepository.AddAsync(user, cancellationToken);

        // 6. Assign roles
        using var connection = _connectionFactory.CreateConnection();
        
        foreach (var roleName in request.Roles)
        {
            // Get role ID
            var roleId = await connection.ExecuteScalarAsync<int?>(
                "SELECT Id FROM Roles WHERE Name = @RoleName",
                new { RoleName = roleName });

            if (roleId == null)
            {
                throw new InvalidOperationException($"Role '{roleName}' not found");
            }

            // Assign role
            var userRoleSql = @"
                INSERT INTO UserRoles (UserId, RoleId, TenantId, AssignedAt)
                VALUES (@UserId, @RoleId, @TenantId, @AssignedAt)";

            await connection.ExecuteAsync(userRoleSql, new
            {
                UserId = userId,
                RoleId = roleId.Value,
                TenantId = tenantId,
                AssignedAt = DateTime.UtcNow
            });
        }

        // 7. Generate JWT token
        var token = _jwtTokenService.GenerateToken(userId, request.Email, request.Roles);

        // 8. Return response
        return new RegisterUserResponse
        {
            AuthResult = new AuthResult
            {
                Token = token,
                UserId = userId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Roles = request.Roles,
                TenantId = tenantId,
                TenantName = tenant.Name
            },
            Message = "User registered successfully"
        };
    }
}
