namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Request to refresh an access token using a refresh token.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
