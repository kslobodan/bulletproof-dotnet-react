using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Resource entity with tenant-scoped operations
/// </summary>
public interface IResourceRepository : IRepository<Resource>
{
    // Inherits all CRUD operations from IRepository<Resource>
    // All operations are automatically tenant-scoped via BaseRepository
}
