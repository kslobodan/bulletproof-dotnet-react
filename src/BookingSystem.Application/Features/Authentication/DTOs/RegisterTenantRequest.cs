namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Request to register a new tenant (organization) with an admin user.
/// This creates both the tenant and the first user (TenantAdmin role).
/// </summary>
public class RegisterTenantRequest
{
    public string TenantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Plan { get; set; } = "Free"; // Default to Free plan
}
