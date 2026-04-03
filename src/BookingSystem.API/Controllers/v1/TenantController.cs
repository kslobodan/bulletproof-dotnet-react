using Microsoft.AspNetCore.Mvc;
using BookingSystem.Application.Common.Interfaces;
using Asp.Versioning;

namespace BookingSystem.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ITenantContext tenantContext, ILogger<TenantController> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to verify tenant resolution
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetTenantInfo()
    {
        _logger.LogInformation("Getting tenant info");
        
        return Ok(new
        {
            TenantId = _tenantContext.TenantId,
            IsResolved = _tenantContext.IsResolved,
            Message = "Tenant context resolved successfully"
        });
    }
}
