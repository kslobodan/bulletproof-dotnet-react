namespace BookingSystem.Application.Features.Resources.DTOs;

/// <summary>
/// DTO for resource read operations
/// </summary>
public class ResourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
