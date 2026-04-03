namespace BookingSystem.Application.Features.Resources.DTOs;

/// <summary>
/// Response DTO after creating a resource
/// </summary>
public class CreateResourceResponse
{
    public ResourceDto Resource { get; set; } = null!;
    public string Message { get; set; } = "Resource created successfully";
}
