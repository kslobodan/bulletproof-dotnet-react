namespace BookingSystem.Application.Features.Resources.DTOs;

/// <summary>
/// Request DTO for creating a new resource
/// </summary>
public class CreateResourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public int? Capacity { get; set; }
}
