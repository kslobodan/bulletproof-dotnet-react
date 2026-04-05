using BookingSystem.Application.Features.Authentication.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Command to refresh an access token using a valid refresh token.
/// Implements token rotation: old refresh token is revoked, new one is issued.
/// </summary>
public class RefreshAccessTokenCommand : IRequest<RefreshTokenResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
}
