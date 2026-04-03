using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Commands.DeleteResource;

/// <summary>
/// Command to delete a resource
/// </summary>
public class DeleteResourceCommand : IRequest<DeleteResourceResponse>
{
    public Guid Id { get; set; }
}
