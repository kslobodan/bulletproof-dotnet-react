namespace BookingSystem.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID from authentication context
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the current user's email
    /// </summary>
    string Email { get; }

    /// <summary>
    /// Gets whether a user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
