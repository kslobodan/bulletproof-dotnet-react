using BookingSystem.Application.Common.Interfaces;

namespace BookingSystem.Infrastructure.Services;

/// <summary>
/// BCrypt implementation of password hashing
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
