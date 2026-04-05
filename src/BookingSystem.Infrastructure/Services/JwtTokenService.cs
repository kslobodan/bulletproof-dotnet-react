using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookingSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BookingSystem.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Guid userId, string email, IEnumerable<string> roles)
    {
        var secretKey = _configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Generate a cryptographically secure random token (256 bits = 32 bytes)
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        // Convert to Base64 string for easy storage and transmission
        return Convert.ToBase64String(randomBytes);
    }
}
