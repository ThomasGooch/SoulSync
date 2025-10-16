using FluentAssertions;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using Xunit;

namespace SoulSync.Core.Tests.Domain;

public class SubscriptionTests
{
    [Fact]
    public void Subscription_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tier = SubscriptionTier.Premium;
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddMonths(1);
        
        // Act
        var subscription = new Subscription
        {
            UserId = userId,
            Tier = tier,
            StartDate = startDate,
            EndDate = endDate
        };
        
        // Assert
        subscription.Id.Should().NotBeEmpty();
        subscription.UserId.Should().Be(userId);
        subscription.Tier.Should().Be(tier);
        subscription.StartDate.Should().BeCloseTo(startDate, TimeSpan.FromSeconds(1));
        subscription.EndDate.Should().BeCloseTo(endDate, TimeSpan.FromSeconds(1));
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.IsActive.Should().BeTrue();
        subscription.AutoRenew.Should().BeTrue();
        subscription.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void Subscription_WhenCancelled_ShouldUpdateStatusAndAutoRenew()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };
        
        // Act
        subscription.Cancel();
        
        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.AutoRenew.Should().BeFalse();
        subscription.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        subscription.IsActive.Should().BeTrue(); // Still active until end date
    }
    
    [Fact]
    public void Subscription_WhenExpired_ShouldUpdateStatusAndIsActive()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            EndDate = DateTime.UtcNow.AddMonths(-1)
        };
        
        // Act
        subscription.Expire();
        
        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Expired);
        subscription.IsActive.Should().BeFalse();
    }
    
    [Fact]
    public void Subscription_WhenRenewed_ShouldExtendEndDate()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };
        var originalEndDate = subscription.EndDate;
        
        // Act
        subscription.Renew();
        
        // Assert
        subscription.EndDate.Should().BeAfter(originalEndDate);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.IsActive.Should().BeTrue();
    }
    
    [Fact]
    public void Subscription_WhenUpgraded_ShouldUpdateTier()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Basic,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };
        
        // Act
        subscription.Upgrade(SubscriptionTier.Premium);
        
        // Assert
        subscription.Tier.Should().Be(SubscriptionTier.Premium);
    }
    
    [Fact]
    public void Subscription_WhenUpgradingToLowerTier_ShouldThrowException()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };
        
        // Act & Assert
        var act = () => subscription.Upgrade(SubscriptionTier.Basic);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot upgrade to a lower tier*");
    }
    
    [Fact]
    public void Subscription_WhenDowngraded_ShouldUpdateTier()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Elite,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };
        
        // Act
        subscription.Downgrade(SubscriptionTier.Premium);
        
        // Assert
        subscription.Tier.Should().Be(SubscriptionTier.Premium);
    }
    
    [Fact]
    public void Subscription_WhenDowngradingToHigherTier_ShouldThrowException()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Basic,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };
        
        // Act & Assert
        var act = () => subscription.Downgrade(SubscriptionTier.Premium);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot downgrade to a higher tier*");
    }
    
    [Fact]
    public void Subscription_IsExpired_WhenEndDateInPast_ShouldReturnTrue()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            EndDate = DateTime.UtcNow.AddDays(-1)
        };
        
        // Act
        var isExpired = subscription.IsExpired();
        
        // Assert
        isExpired.Should().BeTrue();
    }
    
    [Fact]
    public void Subscription_IsExpired_WhenEndDateInFuture_ShouldReturnFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };
        
        // Act
        var isExpired = subscription.IsExpired();
        
        // Assert
        isExpired.Should().BeFalse();
    }
    
    [Fact]
    public void Subscription_GetDaysRemaining_ShouldReturnCorrectValue()
    {
        // Arrange
        var daysToAdd = 15;
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(daysToAdd)
        };
        
        // Act
        var daysRemaining = subscription.GetDaysRemaining();
        
        // Assert
        daysRemaining.Should().BeInRange(daysToAdd - 1, daysToAdd + 1); // Allow 1 day variance for timing
    }
    
    [Fact]
    public void Subscription_HasFeatureAccess_ForFreeUser_ShouldReturnFalseForPremiumFeatures()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Free,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(10)
        };
        
        // Act & Assert
        subscription.HasFeatureAccess("unlimited_likes").Should().BeFalse();
        subscription.HasFeatureAccess("see_who_liked_you").Should().BeFalse();
        subscription.HasFeatureAccess("priority_support").Should().BeFalse();
    }
    
    [Fact]
    public void Subscription_HasFeatureAccess_ForPremiumUser_ShouldReturnTrueForPremiumFeatures()
    {
        // Arrange
        var subscription = new Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };
        
        // Act & Assert
        subscription.HasFeatureAccess("unlimited_likes").Should().BeTrue();
        subscription.HasFeatureAccess("see_who_liked_you").Should().BeTrue();
    }
}
