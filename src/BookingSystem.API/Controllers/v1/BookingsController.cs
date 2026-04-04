using Asp.Versioning;
using BookingSystem.Application.Features.Bookings.Commands.CreateBooking;
using BookingSystem.Application.Features.Bookings.Commands.UpdateBooking;
using BookingSystem.Application.Features.Bookings.Commands.CancelBooking;
using BookingSystem.Application.Features.Bookings.Commands.ConfirmBooking;
using BookingSystem.Application.Features.Bookings.Commands.DeleteBooking;
using BookingSystem.Application.Features.Bookings.Queries.GetBookingById;
using BookingSystem.Application.Features.Bookings.Queries.GetAllBookings;
using BookingSystem.Application.Features.Bookings.DTOs;
using BookingSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSystem.API.Controllers.v1;

/// <summary>
/// Bookings management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all bookings with pagination and filtering
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="resourceId">Optional filter by resource</param>
    /// <param name="userId">Optional filter by user</param>
    /// <param name="status">Optional filter by status (0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled, 4=Rejected)</param>
    /// <param name="startDate">Optional filter by start date (bookings starting from this date)</param>
    /// <param name="endDate">Optional filter by end date (bookings ending before this date)</param>
    /// <returns>Paginated list of bookings</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? resourceId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] BookingStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetAllBookingsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ResourceId = resourceId,
            UserId = userId,
            Status = status,
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Get a specific booking by ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetBookingByIdQuery { Id = id };

        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    /// <param name="request">Booking creation details</param>
    /// <returns>Created booking</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        var command = new CreateBookingCommand
        {
            ResourceId = request.ResourceId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Title = request.Title,
            Description = request.Description
        };

        var result = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Booking.Id },
            result);
    }

    /// <summary>
    /// Update an existing booking (only Pending bookings can be updated)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Updated booking details</param>
    /// <returns>Updated booking</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UpdateBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookingRequest request)
    {
        var command = new UpdateBookingCommand
        {
            Id = id,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Title = request.Title,
            Description = request.Description,
            Notes = request.Notes
        };

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Cancel a booking (user-initiated cancellation)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Cancellation confirmation</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(CancelBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var command = new CancelBookingCommand { Id = id };

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Confirm a booking (admin/manager action to approve pending bookings)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Confirmation result</returns>
    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "ManagerOrAbove")]
    [ProducesResponseType(typeof(ConfirmBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var command = new ConfirmBookingCommand { Id = id };

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Delete a booking permanently (admin only)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DeleteBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteBookingCommand { Id = id };

        var result = await _mediator.Send(command);

        return Ok(result);
    }
}
