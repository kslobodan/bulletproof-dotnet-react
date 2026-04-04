using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// DTO for booking read operations
/// </summary>
public class BookingDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ResourceId { get; set; }
    public Guid UserId { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    public BookingStatus Status { get; set; }
    public string StatusText => Status.ToString();
    
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Computed properties for client convenience
    public int DurationMinutes => (int)(EndTime - StartTime).TotalMinutes;
    public bool IsUpcoming => StartTime > DateTime.UtcNow && Status == BookingStatus.Confirmed;
    public bool IsPast => EndTime < DateTime.UtcNow;
}
