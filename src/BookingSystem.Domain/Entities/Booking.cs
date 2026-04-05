using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ResourceId { get; set; }
    public Guid UserId { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    public BookingStatus Status { get; set; }
    
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties (not mapped to database with Dapper)
    // public Tenant Tenant { get; set; }
    // public Resource Resource { get; set; }
    // public User User { get; set; }
}
