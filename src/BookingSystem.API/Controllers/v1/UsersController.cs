using Microsoft.AspNetCore.Mvc;
using BookingSystem.Application.Common.Interfaces;
using BookingSystem.Domain.Entities;
using Asp.Versioning;

namespace BookingSystem.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserRepository userRepository,
        ITenantContext tenantContext,
        ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets all users for the current tenant (demonstrates multi-tenant filtering)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        _logger.LogInformation("Getting all users for tenant {TenantId}", _tenantContext.TenantId);
        
        var users = await _userRepository.GetAllAsync();
        
        return Ok(new
        {
            TenantId = _tenantContext.TenantId,
            Count = users.Count(),
            Users = users
        });
    }

    /// <summary>
    /// Gets a user by ID (automatically filtered by tenant)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        
        if (user == null)
        {
            return NotFound(new { Message = "User not found in your tenant" });
        }
        
        return Ok(user);
    }

    /// <summary>
    /// Creates a new user (automatically assigned to current tenant)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Check if email already exists in this tenant
        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            return BadRequest(new { Message = "Email already exists in your tenant" });
        }
        
        var user = new User
        {
            Email = request.Email,
            PasswordHash = HashPassword(request.Password), // In reality, use proper hashing
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true
        };
        
        var userId = await _userRepository.AddAsync(user);
        
        _logger.LogInformation("Created user {UserId} for tenant {TenantId}", userId, _tenantContext.TenantId);
        
        return CreatedAtAction(nameof(GetUserById), new { id = userId }, new { Id = userId });
    }

    /// <summary>
    /// Simple password hashing (use BCrypt/Argon2 in production)
    /// </summary>
    private string HashPassword(string password)
    {
        // For demo purposes only - use proper hashing in production
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
    }
}

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName
);
