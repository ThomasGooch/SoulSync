using SoulSync.Core.Enums;

namespace SoulSync.Core.Domain;

/// <summary>
/// Represents a user's subscription to SoulSync premium features
/// </summary>
public class Subscription
{
    /// <summary>
    /// Unique identifier for the subscription
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// User who owns this subscription
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Subscription tier level
    /// </summary>
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    
    /// <summary>
    /// Current status of the subscription
    /// </summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    
    /// <summary>
    /// When the subscription started
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// When the subscription ends/renews
    /// </summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>
    /// Whether the subscription should automatically renew
    /// </summary>
    public bool AutoRenew { get; set; } = true;
    
    /// <summary>
    /// Whether the subscription is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// When the subscription was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the subscription was cancelled (if applicable)
    /// </summary>
    public DateTime? CancelledAt { get; private set; }
    
    /// <summary>
    /// Navigation property to User
    /// </summary>
    public User? User { get; set; }
    
    // Feature access mapping for each tier
    private static readonly Dictionary<SubscriptionTier, HashSet<string>> TierFeatures = new()
    {
        [SubscriptionTier.Free] = new HashSet<string>
        {
            "basic_matching",
            "limited_likes"
        },
        [SubscriptionTier.Basic] = new HashSet<string>
        {
            "basic_matching",
            "unlimited_likes",
            "see_who_liked_you"
        },
        [SubscriptionTier.Premium] = new HashSet<string>
        {
            "basic_matching",
            "unlimited_likes",
            "see_who_liked_you",
            "advanced_filters",
            "read_receipts",
            "conversation_coaching"
        },
        [SubscriptionTier.Elite] = new HashSet<string>
        {
            "basic_matching",
            "unlimited_likes",
            "see_who_liked_you",
            "advanced_filters",
            "read_receipts",
            "conversation_coaching",
            "priority_support",
            "date_suggestions",
            "relationship_coaching"
        }
    };
    
    /// <summary>
    /// Cancels the subscription, preventing auto-renewal
    /// </summary>
    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
        AutoRenew = false;
        CancelledAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Marks the subscription as expired and deactivates it
    /// </summary>
    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
        IsActive = false;
    }
    
    /// <summary>
    /// Renews the subscription for another billing period
    /// </summary>
    public void Renew()
    {
        var renewalPeriod = GetRenewalPeriod();
        EndDate = DateTime.UtcNow.Add(renewalPeriod);
        Status = SubscriptionStatus.Active;
        IsActive = true;
    }
    
    /// <summary>
    /// Upgrades the subscription to a higher tier
    /// </summary>
    /// <param name="newTier">The tier to upgrade to</param>
    /// <exception cref="InvalidOperationException">Thrown when trying to upgrade to a lower tier</exception>
    public void Upgrade(SubscriptionTier newTier)
    {
        if (newTier <= Tier)
        {
            throw new InvalidOperationException($"Cannot upgrade to a lower tier. Current tier: {Tier}, New tier: {newTier}");
        }
        
        Tier = newTier;
    }
    
    /// <summary>
    /// Downgrades the subscription to a lower tier
    /// </summary>
    /// <param name="newTier">The tier to downgrade to</param>
    /// <exception cref="InvalidOperationException">Thrown when trying to downgrade to a higher tier</exception>
    public void Downgrade(SubscriptionTier newTier)
    {
        if (newTier >= Tier)
        {
            throw new InvalidOperationException($"Cannot downgrade to a higher tier. Current tier: {Tier}, New tier: {newTier}");
        }
        
        Tier = newTier;
    }
    
    /// <summary>
    /// Checks if the subscription has expired based on end date
    /// </summary>
    /// <returns>True if the subscription has expired, false otherwise</returns>
    public bool IsExpired()
    {
        return DateTime.UtcNow > EndDate;
    }
    
    /// <summary>
    /// Gets the number of days remaining in the subscription
    /// </summary>
    /// <returns>Number of days remaining (can be negative if expired)</returns>
    public int GetDaysRemaining()
    {
        var timeRemaining = EndDate - DateTime.UtcNow;
        return (int)Math.Ceiling(timeRemaining.TotalDays);
    }
    
    /// <summary>
    /// Checks if the user has access to a specific feature based on their subscription tier
    /// </summary>
    /// <param name="featureName">Name of the feature to check</param>
    /// <returns>True if the user has access to the feature, false otherwise</returns>
    public bool HasFeatureAccess(string featureName)
    {
        if (!IsActive || IsExpired())
        {
            return TierFeatures[SubscriptionTier.Free].Contains(featureName);
        }
        
        return TierFeatures[Tier].Contains(featureName);
    }
    
    /// <summary>
    /// Gets the renewal period based on the subscription tier
    /// </summary>
    /// <returns>TimeSpan representing the renewal period</returns>
    private TimeSpan GetRenewalPeriod()
    {
        return Tier switch
        {
            SubscriptionTier.Free => TimeSpan.FromDays(365 * 10), // Free never expires (10 years)
            SubscriptionTier.Basic => TimeSpan.FromDays(30), // Monthly
            SubscriptionTier.Premium => TimeSpan.FromDays(30), // Monthly
            SubscriptionTier.Elite => TimeSpan.FromDays(30), // Monthly
            _ => TimeSpan.FromDays(30)
        };
    }
}
