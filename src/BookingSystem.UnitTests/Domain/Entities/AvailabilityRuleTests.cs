using BookingSystem.Domain.Entities;
using FluentAssertions;

namespace BookingSystem.UnitTests.Domain.Entities;

public class AvailabilityRuleTests
{
    [Fact]
    public void AvailabilityRule_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var rule = new AvailabilityRule();

        // Assert
        rule.IsActive.Should().BeTrue();
        rule.IsDeleted.Should().BeFalse();
        rule.DeletedAt.Should().BeNull();
        rule.EffectiveFrom.Should().BeNull();
        rule.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void AvailabilityRule_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var startTime = new TimeSpan(9, 0, 0); // 09:00
        var endTime = new TimeSpan(17, 0, 0); // 17:00
        var createdAt = DateTime.UtcNow;
        var effectiveFrom = DateTime.UtcNow.Date;
        var effectiveTo = DateTime.UtcNow.Date.AddMonths(6);

        // Act
        var rule = new AvailabilityRule
        {
            Id = id,
            TenantId = tenantId,
            ResourceId = resourceId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = true,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedAt = createdAt
        };

        // Assert
        rule.Id.Should().Be(id);
        rule.TenantId.Should().Be(tenantId);
        rule.ResourceId.Should().Be(resourceId);
        rule.DayOfWeek.Should().Be(DayOfWeek.Monday);
        rule.StartTime.Should().Be(startTime);
        rule.EndTime.Should().Be(endTime);
        rule.IsActive.Should().BeTrue();
        rule.EffectiveFrom.Should().Be(effectiveFrom);
        rule.EffectiveTo.Should().Be(effectiveTo);
        rule.CreatedAt.Should().Be(createdAt);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    public void AvailabilityRule_ShouldSupportAllDaysOfWeek(DayOfWeek dayOfWeek)
    {
        // Arrange
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };

        // Act
        rule.DayOfWeek = dayOfWeek;

        // Assert
        rule.DayOfWeek.Should().Be(dayOfWeek);
    }

    [Fact]
    public void AvailabilityRule_ShouldSupportTimeSpanForOperatingHours()
    {
        // Arrange
        var startTime = new TimeSpan(8, 30, 0); // 08:30:00
        var endTime = new TimeSpan(18, 45, 30); // 18:45:30

        // Act
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = startTime,
            EndTime = endTime
        };

        // Assert
        rule.StartTime.Should().Be(startTime);
        rule.StartTime.Hours.Should().Be(8);
        rule.StartTime.Minutes.Should().Be(30);
        rule.StartTime.Seconds.Should().Be(0);
        
        rule.EndTime.Should().Be(endTime);
        rule.EndTime.Hours.Should().Be(18);
        rule.EndTime.Minutes.Should().Be(45);
        rule.EndTime.Seconds.Should().Be(30);
    }

    [Fact]
    public void AvailabilityRule_ShouldSupportActiveInactiveState()
    {
        // Arrange
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsActive = true
        };

        // Act
        rule.IsActive = false;

        // Assert
        rule.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AvailabilityRule_ShouldSupportEffectiveDateRange()
    {
        // Arrange
        var effectiveFrom = new DateTime(2026, 1, 1);
        var effectiveTo = new DateTime(2026, 12, 31);

        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };

        // Act
        rule.EffectiveFrom = effectiveFrom;
        rule.EffectiveTo = effectiveTo;

        // Assert
        rule.EffectiveFrom.Should().Be(effectiveFrom);
        rule.EffectiveTo.Should().Be(effectiveTo);
    }

    [Fact]
    public void AvailabilityRule_ShouldSupportSoftDelete()
    {
        // Arrange
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0)
        };

        var deletedAt = DateTime.UtcNow;

        // Act
        rule.IsDeleted = true;
        rule.DeletedAt = deletedAt;

        // Assert
        rule.IsDeleted.Should().BeTrue();
        rule.DeletedAt.Should().Be(deletedAt);
    }

    [Fact]
    public void AvailabilityRule_ShouldAllowOpenEndedEffectivePeriod()
    {
        // Arrange & Act
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Thursday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            EffectiveFrom = DateTime.UtcNow.Date,
            EffectiveTo = null // Open-ended
        };

        // Assert
        rule.EffectiveFrom.Should().NotBeNull();
        rule.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void AvailabilityRule_ShouldTrackUpdateMetadata()
    {
        // Arrange
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Saturday,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(14, 0, 0),
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        var updatedAt = DateTime.UtcNow;

        // Act
        rule.UpdatedAt = updatedAt;

        // Assert
        rule.UpdatedAt.Should().Be(updatedAt);
    }
}
