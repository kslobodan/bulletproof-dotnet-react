namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Login request with email and password.
/// Email is used to identify both the tenant and the user.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
