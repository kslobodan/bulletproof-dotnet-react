namespace BookingSystem.Application.Features.AvailabilityRules.DTOs;

public class AvailabilityRuleDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public string DayOfWeekText { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string StartTimeText { get; set; } = string.Empty;
    public string EndTimeText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DurationMinutes { get; set; }
}

public class CreateAvailabilityRuleRequest
{
    public Guid ResourceId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class CreateAvailabilityRuleResponse
{
    public AvailabilityRuleDto AvailabilityRule { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
}

public class UpdateAvailabilityRuleRequest
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class UpdateAvailabilityRuleResponse
{
    public AvailabilityRuleDto AvailabilityRule { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
}

public class DeleteAvailabilityRuleResponse
{
    public string Message { get; set; } = string.Empty;
}
