using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Commands.UpdateResource;

/// <summary>
/// Command to update an existing resource
/// </summary>
public class UpdateResourceCommand : IRequest<UpdateResourceResponse>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public int? Capacity { get; set; }
    public bool IsActive { get; set; }
}
