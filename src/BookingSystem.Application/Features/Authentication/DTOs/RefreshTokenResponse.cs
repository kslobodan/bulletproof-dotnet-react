namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Response containing new access token and refresh token.
/// </summary>
public class RefreshTokenResponse
{
    public AuthResult? AuthResult { get; set; }
    public string Message { get; set; } = string.Empty;
}
