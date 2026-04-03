using BookingSystem.Application.Features.Authentication.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Authentication.Commands.RegisterTenant;

/// <summary>
/// Command to register a new tenant (organization) with its first admin user.
/// Uses CQRS pattern via MediatR.
/// </summary>
public class RegisterTenantCommand : IRequest<RegisterTenantResponse>
{
    public string TenantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Plan { get; set; } = "Free";
}
