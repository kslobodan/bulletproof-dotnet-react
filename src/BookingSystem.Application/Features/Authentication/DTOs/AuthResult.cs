namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Authentication result containing JWT token and user information.
/// Returned after successful login or registration.
/// </summary>
public class AuthResult
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
}
