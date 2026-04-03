using BookingSystem.Application.Common.Models;
using BookingSystem.Application.Features.Resources.DTOs;
using MediatR;

namespace BookingSystem.Application.Features.Resources.Queries.GetAllResources;

/// <summary>
/// Query to get all resources with pagination and optional filtering
/// </summary>
public class GetAllResourcesQuery : IRequest<PagedResult<ResourceDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? ResourceType { get; set; }
    public bool? IsActive { get; set; }
}
