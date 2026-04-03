namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Request to register a new user within an existing tenant.
/// Requires X-Tenant-Id header to identify the tenant.
/// </summary>
public class RegisterUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new() { "User" }; // Default to User role
}
