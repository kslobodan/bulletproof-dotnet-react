using BookingSystem.Domain.Entities;
using FluentAssertions;

namespace BookingSystem.UnitTests.Domain.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpiresAtIsInThePast()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-token",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            IsRevoked = false
        };

        // Act
        var isExpired = refreshToken.IsExpired;

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldReturnFalse_WhenExpiresAtIsInTheFuture()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-token",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Expires in 7 days
            IsRevoked = false
        };

        // Act
        var isExpired = refreshToken.IsExpired;

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpiresAtIsExactlyNow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-token",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = now.AddDays(-7),
            ExpiresAt = now, // Expires exactly now
            IsRevoked = false
        };

        // Act
        var isExpired = refreshToken.IsExpired;

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldReturnTrue_WhenNotRevokedAndNotExpired()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-token",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenRevoked()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-token",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true, // Revoked
            RevokedAt = DateTime.UtcNow
        };

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenExpired()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-token",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = false
        };

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenBothRevokedAndExpired()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "test-token",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = true, // Also revoked
            RevokedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Act
        var isActive = refreshToken.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_ShouldInitializeWithEmptyToken()
    {
        // Arrange & Act
        var refreshToken = new RefreshToken();

        // Assert
        refreshToken.Token.Should().Be(string.Empty);
    }

    [Fact]
    public void RefreshToken_ShouldAllowSettingReplacedByToken()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "old-token",
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var newToken = "new-replacement-token";

        // Act
        refreshToken.ReplacedByToken = newToken;

        // Assert
        refreshToken.ReplacedByToken.Should().Be(newToken);
    }
}
