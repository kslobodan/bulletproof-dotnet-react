namespace BookingSystem.Application.Features.Authentication.DTOs;

/// <summary>
/// Response after successful tenant registration.
/// Contains authentication token and tenant/user information.
/// </summary>
public class RegisterTenantResponse
{
    public AuthResult AuthResult { get; set; } = new();
    public string Message { get; set; } = "Tenant registered successfully";
}
