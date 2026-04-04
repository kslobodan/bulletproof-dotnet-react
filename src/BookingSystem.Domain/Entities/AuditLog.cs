namespace BookingSystem.Domain.Entities;

/// <summary>
/// Audit log entity for tracking all data changes (Create, Update, Delete operations)
/// Provides compliance and security by recording who changed what and when
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant isolation
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Type of entity being audited (e.g., "Booking", "Resource", "User")
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity being audited
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Action performed: "Create", "Update", "Delete"
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// JSON representation of the entity BEFORE the change (null for Create)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of the entity AFTER the change (null for Delete)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Timestamp of the action (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// IP address of the user (optional, for security audits)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Additional context or reason for the change (optional)
    /// </summary>
    public string? Reason { get; set; }
}
