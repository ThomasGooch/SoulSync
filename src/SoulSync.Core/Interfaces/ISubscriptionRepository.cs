using SoulSync.Core.Domain;
using SoulSync.Core.Enums;

namespace SoulSync.Core.Interfaces;

/// <summary>
/// Repository interface for managing subscription data persistence
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>
    /// Gets a subscription by its ID
    /// </summary>
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the active subscription for a user
    /// </summary>
    Task<Subscription?> GetActiveSubscriptionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all subscriptions for a user (including historical)
    /// </summary>
    Task<IEnumerable<Subscription>> GetSubscriptionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new subscription
    /// </summary>
    Task<Subscription> CreateAsync(Subscription subscription, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing subscription
    /// </summary>
    Task<Subscription> UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all subscriptions that are due for renewal
    /// </summary>
    Task<IEnumerable<Subscription>> GetSubscriptionsDueForRenewalAsync(int daysAhead = 1, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all expired subscriptions
    /// </summary>
    Task<IEnumerable<Subscription>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
}
