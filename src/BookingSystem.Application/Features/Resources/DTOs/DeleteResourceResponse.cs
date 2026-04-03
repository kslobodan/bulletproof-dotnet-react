namespace BookingSystem.Application.Features.Resources.DTOs;

/// <summary>
/// Response DTO after deleting a resource
/// </summary>
public class DeleteResourceResponse
{
    public string Message { get; set; } = "Resource deleted successfully";
}
