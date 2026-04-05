namespace BookingSystem.Application.Features.Bookings.DTOs;

/// <summary>
/// DTO containing booking statistics for a tenant
/// </summary>
public class BookingStatisticsDto
{
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    
    public decimal ConfirmationRate { get; set; } // Percentage of bookings confirmed
    public decimal CancellationRate { get; set; } // Percentage of bookings cancelled
    
    public List<ResourceUtilizationDto> ResourceUtilization { get; set; } = new();
    public List<PopularTimeSlotDto> PopularTimeSlots { get; set; } = new();
}

/// <summary>
/// Resource utilization statistics
/// </summary>
public class ResourceUtilizationDto
{
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public decimal UtilizationPercentage { get; set; } // Based on analysis period
}

/// <summary>
/// Popular time slots based on booking frequency
/// </summary>
public class PopularTimeSlotDto
{
    public int HourOfDay { get; set; } // 0-23
    public int BookingCount { get; set; }
    public string TimeSlotLabel { get; set; } = string.Empty; // e.g., "09:00-10:00"
}
