namespace BookingSystem.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
}
