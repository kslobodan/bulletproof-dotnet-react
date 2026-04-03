namespace BookingSystem.Domain.Entities;

/// <summary>
/// Tenant entity - represents an organization/company in the multi-tenant system
/// </summary>
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty; // e.g., "Free", "Pro", "Enterprise"
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
