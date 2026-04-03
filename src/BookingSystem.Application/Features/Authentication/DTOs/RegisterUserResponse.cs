namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Response after successful user registration.
/// Contains authentication token and user information.
/// </summary>
public class RegisterUserResponse
{
    public AuthResult AuthResult { get; set; } = new();
    public string Message { get; set; } = "User registered successfully";
}
