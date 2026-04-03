using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Queries.GetResourceById;

/// <summary>
/// Query to get a single resource by ID
/// </summary>
public class GetResourceByIdQuery : IRequest<ResourceDto>
{
    public Guid Id { get; set; }
}
