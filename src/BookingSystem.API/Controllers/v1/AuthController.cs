using Asp.Versioning;
using BookingSystem.Application.Features.Authentication.Commands.Login;
using BookingSystem.Application.Features.Authentication.Commands.RefreshToken;
using BookingSystem.Application.Features.Authentication.Commands.RegisterTenant;
using BookingSystem.Application.Features.Authentication.Commands.RegisterUser;
using BookingSystem.Application.Features.Authentication.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BookingSystem.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Register a new tenant (organization) with its first admin user.
    /// Creates both tenant and user in a single operation.
    /// </summary>
    [HttpPost("register-tenant")]
    [ProducesResponseType(typeof(RegisterTenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request)
    {
        var command = new RegisterTenantCommand
        {
            TenantName = request.TenantName,
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Plan = request.Plan
        };

        var response = await _mediator.Send(command);
        
        return CreatedAtAction(nameof(RegisterTenant), response);
    }

    /// <summary>
    /// Register a new user within an existing tenant.
    /// Requires X-Tenant-Id header to identify the tenant.
    /// </summary>
    [HttpPost("register-user")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
    {
        var command = new RegisterUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Roles = request.Roles
        };

        var response = await _mediator.Send(command);
        
        return CreatedAtAction(nameof(RegisterUser), response);
    }

    /// <summary>
    /// Authenticate a user with email and password.
    /// Returns JWT token and user information.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var response = await _mediator.Send(command);
        
        return Ok(response);
    }

    /// <summary>
    /// Refresh an access token using a valid refresh token.
    /// Returns new JWT access token and new refresh token (token rotation).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshAccessTokenCommand
        {
            RefreshToken = request.RefreshToken
        };

        var response = await _mediator.Send(command);
        
        return Ok(response);
    }
}
