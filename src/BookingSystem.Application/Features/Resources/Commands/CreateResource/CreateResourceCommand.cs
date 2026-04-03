using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Commands.CreateResource;

/// <summary>
/// Command to create a new resource
/// </summary>
public class CreateResourceCommand : IRequest<CreateResourceResponse>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public int? Capacity { get; set; }
}
