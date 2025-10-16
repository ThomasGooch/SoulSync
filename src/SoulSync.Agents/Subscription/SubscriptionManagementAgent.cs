using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Subscription;

/// <summary>
/// Agent responsible for managing user subscriptions including creation, upgrades, downgrades, and cancellations
/// </summary>
public class SubscriptionManagementAgent : BaseAgent
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SubscriptionManagementAgent> _logger;
    
    public SubscriptionManagementAgent(
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        ILogger<SubscriptionManagementAgent> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
        _logger = logger;
    }
    
    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        // Get the action to perform
        if (!request.Parameters.TryGetValue("action", out var actionObj) || actionObj is not string action)
        {
            return AgentResult.CreateError("Missing required parameter: action");
        }
        
        // Route to appropriate handler based on action
        return action.ToLower() switch
        {
            "create" => await CreateSubscriptionAsync(request.Parameters, cancellationToken),
            "cancel" => await CancelSubscriptionAsync(request.Parameters, cancellationToken),
            "upgrade" => await UpgradeSubscriptionAsync(request.Parameters, cancellationToken),
            "downgrade" => await DowngradeSubscriptionAsync(request.Parameters, cancellationToken),
            "renew" => await RenewSubscriptionAsync(request.Parameters, cancellationToken),
            "get" => await GetSubscriptionAsync(request.Parameters, cancellationToken),
            _ => AgentResult.CreateError($"Invalid action: {action}. Valid actions are: create, cancel, upgrade, downgrade, renew, get")
        };
    }
    
    private async Task<AgentResult> CreateSubscriptionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        // Validate required parameters
        if (!parameters.TryGetValue("userId", out var userIdObj) || !Guid.TryParse(userIdObj.ToString(), out var userId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: userId");
        }
        
        if (!parameters.TryGetValue("tier", out var tierObj) || !Enum.TryParse<SubscriptionTier>(tierObj.ToString(), true, out var tier))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: tier");
        }
        
        // Verify user exists
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return AgentResult.CreateError($"User not found with ID: {userId}");
        }
        
        // Get duration (default to 1 month)
        var durationMonths = 1;
        if (parameters.TryGetValue("durationMonths", out var durationObj) && int.TryParse(durationObj.ToString(), out var duration))
        {
            durationMonths = duration;
        }
        
        // Create subscription
        var startDate = DateTime.UtcNow;
        var subscription = new Core.Domain.Subscription
        {
            UserId = userId,
            Tier = tier,
            StartDate = startDate,
            EndDate = startDate.AddMonths(durationMonths),
            Status = SubscriptionStatus.Active,
            AutoRenew = true,
            IsActive = true
        };
        
        var createdSubscription = await _subscriptionRepository.CreateAsync(subscription, cancellationToken);
        
        _logger.LogInformation("Created subscription {SubscriptionId} for user {UserId} with tier {Tier}", 
            createdSubscription.Id, userId, tier);
        
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["subscriptionId"] = createdSubscription.Id,
            ["userId"] = createdSubscription.UserId,
            ["tier"] = createdSubscription.Tier.ToString(),
            ["status"] = createdSubscription.Status.ToString(),
            ["startDate"] = createdSubscription.StartDate,
            ["endDate"] = createdSubscription.EndDate,
            ["autoRenew"] = createdSubscription.AutoRenew
        });
    }
    
    private async Task<AgentResult> CancelSubscriptionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        // Validate required parameters
        if (!parameters.TryGetValue("subscriptionId", out var subscriptionIdObj) || !Guid.TryParse(subscriptionIdObj.ToString(), out var subscriptionId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: subscriptionId");
        }
        
        // Get subscription
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            return AgentResult.CreateError($"Subscription not found with ID: {subscriptionId}");
        }
        
        // Cancel subscription
        subscription.Cancel();
        
        var updatedSubscription = await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        
        _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscriptionId);
        
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["subscriptionId"] = updatedSubscription.Id,
            ["status"] = updatedSubscription.Status.ToString(),
            ["cancelledAt"] = updatedSubscription.CancelledAt!,
            ["endDate"] = updatedSubscription.EndDate
        });
    }
    
    private async Task<AgentResult> UpgradeSubscriptionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        // Validate required parameters
        if (!parameters.TryGetValue("subscriptionId", out var subscriptionIdObj) || !Guid.TryParse(subscriptionIdObj.ToString(), out var subscriptionId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: subscriptionId");
        }
        
        if (!parameters.TryGetValue("newTier", out var newTierObj) || !Enum.TryParse<SubscriptionTier>(newTierObj.ToString(), true, out var newTier))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: newTier");
        }
        
        // Get subscription
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            return AgentResult.CreateError($"Subscription not found with ID: {subscriptionId}");
        }
        
        try
        {
            // Upgrade subscription
            subscription.Upgrade(newTier);
            
            var updatedSubscription = await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            
            _logger.LogInformation("Upgraded subscription {SubscriptionId} to tier {NewTier}", subscriptionId, newTier);
            
            return AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["subscriptionId"] = updatedSubscription.Id,
                ["tier"] = updatedSubscription.Tier.ToString(),
                ["status"] = updatedSubscription.Status.ToString()
            });
        }
        catch (InvalidOperationException ex)
        {
            return AgentResult.CreateError(ex.Message);
        }
    }
    
    private async Task<AgentResult> DowngradeSubscriptionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        // Validate required parameters
        if (!parameters.TryGetValue("subscriptionId", out var subscriptionIdObj) || !Guid.TryParse(subscriptionIdObj.ToString(), out var subscriptionId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: subscriptionId");
        }
        
        if (!parameters.TryGetValue("newTier", out var newTierObj) || !Enum.TryParse<SubscriptionTier>(newTierObj.ToString(), true, out var newTier))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: newTier");
        }
        
        // Get subscription
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            return AgentResult.CreateError($"Subscription not found with ID: {subscriptionId}");
        }
        
        try
        {
            // Downgrade subscription
            subscription.Downgrade(newTier);
            
            var updatedSubscription = await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            
            _logger.LogInformation("Downgraded subscription {SubscriptionId} to tier {NewTier}", subscriptionId, newTier);
            
            return AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["subscriptionId"] = updatedSubscription.Id,
                ["tier"] = updatedSubscription.Tier.ToString(),
                ["status"] = updatedSubscription.Status.ToString()
            });
        }
        catch (InvalidOperationException ex)
        {
            return AgentResult.CreateError(ex.Message);
        }
    }
    
    private async Task<AgentResult> RenewSubscriptionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        // Validate required parameters
        if (!parameters.TryGetValue("subscriptionId", out var subscriptionIdObj) || !Guid.TryParse(subscriptionIdObj.ToString(), out var subscriptionId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: subscriptionId");
        }
        
        // Get subscription
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            return AgentResult.CreateError($"Subscription not found with ID: {subscriptionId}");
        }
        
        // Renew subscription
        subscription.Renew();
        
        var updatedSubscription = await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        
        _logger.LogInformation("Renewed subscription {SubscriptionId} until {EndDate}", subscriptionId, updatedSubscription.EndDate);
        
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["subscriptionId"] = updatedSubscription.Id,
            ["status"] = updatedSubscription.Status.ToString(),
            ["newEndDate"] = updatedSubscription.EndDate
        });
    }
    
    private async Task<AgentResult> GetSubscriptionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        // Check if getting by subscriptionId or userId
        if (parameters.TryGetValue("subscriptionId", out var subscriptionIdObj) && Guid.TryParse(subscriptionIdObj.ToString(), out var subscriptionId))
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
            if (subscription == null)
            {
                return AgentResult.CreateError($"Subscription not found with ID: {subscriptionId}");
            }
            
            return BuildSubscriptionResult(subscription);
        }
        
        if (parameters.TryGetValue("userId", out var userIdObj) && Guid.TryParse(userIdObj.ToString(), out var userId))
        {
            var subscription = await _subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId, cancellationToken);
            if (subscription == null)
            {
                return AgentResult.CreateError($"No active subscription found for user: {userId}");
            }
            
            return BuildSubscriptionResult(subscription);
        }
        
        return AgentResult.CreateError("Missing required parameter: subscriptionId or userId");
    }
    
    private static AgentResult BuildSubscriptionResult(Core.Domain.Subscription subscription)
    {
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["subscriptionId"] = subscription.Id,
            ["userId"] = subscription.UserId,
            ["tier"] = subscription.Tier.ToString(),
            ["status"] = subscription.Status.ToString(),
            ["startDate"] = subscription.StartDate,
            ["endDate"] = subscription.EndDate,
            ["autoRenew"] = subscription.AutoRenew,
            ["isActive"] = subscription.IsActive,
            ["daysRemaining"] = subscription.GetDaysRemaining(),
            ["isExpired"] = subscription.IsExpired()
        });
    }
}
