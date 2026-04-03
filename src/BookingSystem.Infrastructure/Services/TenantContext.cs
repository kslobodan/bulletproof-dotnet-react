using BookingSystem.Application.Common.Interfaces;

namespace BookingSystem.Infrastructure.Services;

/// <summary>
/// Implementation of tenant context for multi-tenant data isolation.
/// Stores the current tenant ID resolved from HTTP headers or subdomain.
/// </summary>
public class TenantContext : ITenantContext
{
    private Guid _tenantId;
    private bool _isResolved;

    public Guid TenantId
    {
        get => _isResolved ? _tenantId : throw new InvalidOperationException("Tenant context has not been resolved");
    }

    public bool IsResolved => _isResolved;

    /// <summary>
    /// Sets the current tenant identifier
    /// </summary>
    /// <param name="tenantId">The tenant's unique identifier</param>
    public void SetTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
        _isResolved = true;
    }

    /// <summary>
    /// Clears the tenant context (useful for testing or background jobs)
    /// </summary>
    public void Clear()
    {
        _tenantId = Guid.Empty;
        _isResolved = false;
    }
}
