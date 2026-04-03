namespace BookingSystem.Application.Features.Resources.DTOs;

/// <summary>
/// Request DTO for updating an existing resource
/// </summary>
public class UpdateResourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; }
}
