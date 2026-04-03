namespace BookingSystem.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current tenant's identifier for multi-tenant data isolation
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant's unique identifier
    /// </summary>
    Guid TenantId { get; }
    
    /// <summary>
    /// Indicates whether a tenant context has been successfully resolved
    /// </summary>
    bool IsResolved { get; }
}
