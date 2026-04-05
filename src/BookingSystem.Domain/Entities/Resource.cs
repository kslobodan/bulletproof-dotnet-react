namespace BookingSystem.Domain.Entities;

/// <summary>
/// Represents a bookable resource (meeting room, equipment, appointment slot, etc.)
/// </summary>
public class Resource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ResourceType { get; set; } = string.Empty; // e.g., "MeetingRoom", "Equipment", "Doctor"
    public int? Capacity { get; set; } // e.g., number of people for a meeting room
    public bool IsActive { get; set; } = true;
    
    // Multi-tenancy
    public Guid TenantId { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
