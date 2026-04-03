namespace BookingSystem.Application.Features.Resources.DTOs;

/// <summary>
/// Response DTO after updating a resource
/// </summary>
public class UpdateResourceResponse
{
    public ResourceDto Resource { get; set; } = null!;
    public string Message { get; set; } = "Resource updated successfully";
}
