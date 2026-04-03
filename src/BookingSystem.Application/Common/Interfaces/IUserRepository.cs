using BookingSystem.Domain.Entities;

namespace BookingSystem.Application.Common.Interfaces;

/// <summary>
/// Repository interface for User entity operations.
/// All operations are automatically scoped to the current tenant.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by email within the current tenant
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user with the given email exists within the current tenant
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}
