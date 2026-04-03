using BookingSystem.Application.Features.Authentication.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Command to authenticate a user with email and password.
/// Email is used to identify both the tenant and the user.
/// </summary>
public class LoginCommand : IRequest<LoginResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
