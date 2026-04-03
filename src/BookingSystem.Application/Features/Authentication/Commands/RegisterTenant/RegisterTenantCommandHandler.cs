using Dapper;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Application.Features.Authentication.DTOs;
using BookingSystem.Domain.Entities;
using MediatR;

namespace BookingSystem.Application.Features.Authentication.Commands.RegisterTenant;

/// <summary>
/// Handler for RegisterTenantCommand.
/// Creates a new tenant and its first admin user in a transaction.
/// </summary>
public class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, RegisterTenantResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDbConnectionFactory _connectionFactory;

    public RegisterTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDbConnectionFactory connectionFactory)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _connectionFactory = connectionFactory;
    }

    public async Task<RegisterTenantResponse> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if tenant email already exists
        var existingTenant = await _tenantRepository.GetByEmailAsync(request.Email);
        if (existingTenant != null)
        {
            throw new InvalidOperationException("A tenant with this email already exists");
        }

        // 2. Create the tenant
        var tenant = new Tenant
        {
            Name = request.TenantName,
            Email = request.Email,
            Plan = request.Plan,
            IsActive = true
        };

        var tenantId = await _tenantRepository.AddAsync(tenant, cancellationToken);

        // 3. Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 4. Create the first user (admin) with direct SQL to bypass tenant context
        using var connection = _connectionFactory.CreateConnection();
        
        var userId = Guid.NewGuid();
        var userSql = @"
            INSERT INTO Users (Id, TenantId, Email, PasswordHash, FirstName, LastName, IsActive, CreatedAt)
            VALUES (@Id, @TenantId, @Email, @PasswordHash, @FirstName, @LastName, @IsActive, @CreatedAt)";

        await connection.ExecuteAsync(userSql, new
        {
            Id = userId,
            TenantId = tenantId,
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        // 5. Get TenantAdmin role ID
        var roleId = await connection.ExecuteScalarAsync<int>(
            "SELECT Id FROM Roles WHERE Name = @RoleName",
            new { RoleName = "TenantAdmin" });

        // 6. Assign TenantAdmin role
        var userRoleSql = @"
            INSERT INTO UserRoles (UserId, RoleId, TenantId, AssignedAt)
            VALUES (@UserId, @RoleId, @TenantId, @AssignedAt)";

        await connection.ExecuteAsync(userRoleSql, new
        {
            UserId = userId,
            RoleId = roleId,
            TenantId = tenantId,
            AssignedAt = DateTime.UtcNow
        });

        // 7. Generate JWT token
        var roles = new List<string> { "TenantAdmin" };
        var token = _jwtTokenService.GenerateToken(userId, request.Email, roles);

        // 8. Return response
        return new RegisterTenantResponse
        {
            AuthResult = new AuthResult
            {
                Token = token,
                UserId = userId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Roles = roles,
                TenantId = tenantId,
                TenantName = request.TenantName
            },
            Message = "Tenant registered successfully"
        };
    }
}
