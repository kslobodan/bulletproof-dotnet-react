using BookingSystem.Application.Features.Authentication.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// Command to register a new user within an existing tenant.
/// Requires tenant context (X-Tenant-Id header).
/// </summary>
public class RegisterUserCommand : IRequest<RegisterUserResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new() { "User" };
}
