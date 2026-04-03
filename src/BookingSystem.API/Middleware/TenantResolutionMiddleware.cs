using BookingSystem.Infrastructure.Services;

namespace BookingSystem.API.Middleware;

/// <summary>
/// Middleware that resolves the current tenant from HTTP headers.
/// Expects 'X-Tenant-Id' header with a valid GUID.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // Skip tenant resolution for health checks and Swagger endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (path.Contains("/swagger") || path.Contains("/health")))
        {
            await _next(context);
            return;
        }

        // Try to get tenant ID from header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            if (Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                tenantContext.SetTenantId(tenantId);
                _logger.LogDebug("Tenant resolved: {TenantId}", tenantId);
            }
            else
            {
                _logger.LogWarning("Invalid X-Tenant-Id header value: {TenantIdHeader}", tenantIdHeader.ToString());
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    StatusCode = 400,
                    Message = "Invalid X-Tenant-Id header format. Expected a valid GUID."
                });
                return;
            }
        }
        else
        {
            // No tenant header provided for non-public endpoints
            _logger.LogWarning("Missing X-Tenant-Id header for request: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                StatusCode = 400,
                Message = "X-Tenant-Id header is required."
            });
            return;
        }

        await _next(context);
    }
}
