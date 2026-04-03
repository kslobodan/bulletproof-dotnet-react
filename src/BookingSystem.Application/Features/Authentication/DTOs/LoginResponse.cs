namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Response after successful login.
/// Contains JWT token and user information.
/// </summary>
public class LoginResponse
{
    public AuthResult AuthResult { get; set; } = new();
    public string Message { get; set; } = "Login successful";
}
