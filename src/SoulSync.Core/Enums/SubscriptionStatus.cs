namespace SoulSync.Core.Enums;

/// <summary>
/// Represents the status of a subscription
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and in good standing
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Subscription payment is past due
    /// </summary>
    PastDue = 1,
    
    /// <summary>
    /// Subscription has been canceled but is still active until period end
    /// </summary>
    Cancelled = 2,
    
    /// <summary>
    /// Subscription has expired
    /// </summary>
    Expired = 3,
    
    /// <summary>
    /// Subscription is in trial period
    /// </summary>
    Trial = 4
}
