using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Subscription;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;
using Xunit;

namespace SoulSync.Agents.Tests;

public class SubscriptionManagementAgentTests
{
    private readonly ISubscriptionRepository _mockSubscriptionRepository;
    private readonly IUserRepository _mockUserRepository;
    private readonly ILogger<SubscriptionManagementAgent> _mockLogger;
    private readonly SubscriptionManagementAgent _agent;
    
    public SubscriptionManagementAgentTests()
    {
        _mockSubscriptionRepository = Substitute.For<ISubscriptionRepository>();
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockLogger = Substitute.For<ILogger<SubscriptionManagementAgent>>();
        
        _agent = new SubscriptionManagementAgent(
            _mockSubscriptionRepository,
            _mockUserRepository,
            _mockLogger);
    }
    
    [Fact]
    public async Task ExecuteAsync_CreateSubscription_WithValidData_ShouldCreateSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", FirstName = "Test", LastName = "User" };
        
        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);
        
        _mockSubscriptionRepository.CreateAsync(Arg.Any<Core.Domain.Subscription>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<Core.Domain.Subscription>());
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "create",
                ["userId"] = userId.ToString(),
                ["tier"] = "Premium",
                ["durationMonths"] = 1
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        data!.Should().ContainKey("subscriptionId");
        data.Should().ContainKey("tier");
        data.Should().ContainKey("startDate");
        data.Should().ContainKey("endDate");
        
        await _mockSubscriptionRepository.Received(1).CreateAsync(
            Arg.Is<Core.Domain.Subscription>(s => 
                s.UserId == userId && 
                s.Tier == SubscriptionTier.Premium),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ExecuteAsync_CreateSubscription_WithMissingUserId_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "create",
                ["tier"] = "Premium"
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("userId");
    }
    
    [Fact]
    public async Task ExecuteAsync_CreateSubscription_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "create",
                ["userId"] = userId.ToString(),
                ["tier"] = "Premium"
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }
    
    [Fact]
    public async Task ExecuteAsync_CancelSubscription_WithValidSubscription_ShouldCancelSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = new Core.Domain.Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Status = SubscriptionStatus.Active
        };
        
        _mockSubscriptionRepository.GetByIdAsync(subscriptionId, Arg.Any<CancellationToken>())
            .Returns(subscription);
        
        _mockSubscriptionRepository.UpdateAsync(Arg.Any<Core.Domain.Subscription>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<Core.Domain.Subscription>());
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "cancel",
                ["subscriptionId"] = subscriptionId.ToString()
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        data!["status"].Should().Be("Cancelled");
        
        await _mockSubscriptionRepository.Received(1).UpdateAsync(
            Arg.Is<Core.Domain.Subscription>(s => s.Status == SubscriptionStatus.Cancelled),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ExecuteAsync_UpgradeSubscription_WithValidData_ShouldUpgradeSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = new Core.Domain.Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Basic,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Status = SubscriptionStatus.Active
        };
        
        _mockSubscriptionRepository.GetByIdAsync(subscriptionId, Arg.Any<CancellationToken>())
            .Returns(subscription);
        
        _mockSubscriptionRepository.UpdateAsync(Arg.Any<Core.Domain.Subscription>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<Core.Domain.Subscription>());
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "upgrade",
                ["subscriptionId"] = subscriptionId.ToString(),
                ["newTier"] = "Premium"
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        data!["tier"].Should().Be("Premium");
        
        await _mockSubscriptionRepository.Received(1).UpdateAsync(
            Arg.Is<Core.Domain.Subscription>(s => s.Tier == SubscriptionTier.Premium),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ExecuteAsync_GetSubscription_WithValidUserId_ShouldReturnSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subscription = new Core.Domain.Subscription
        {
            UserId = userId,
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Status = SubscriptionStatus.Active
        };
        
        _mockSubscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(subscription);
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "get",
                ["userId"] = userId.ToString()
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        data!.Should().ContainKey("subscriptionId");
        data["tier"].Should().Be("Premium");
        data["status"].Should().Be("Active");
    }
    
    [Fact]
    public async Task ExecuteAsync_RenewSubscription_WithValidSubscription_ShouldRenewSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = new Core.Domain.Subscription
        {
            UserId = Guid.NewGuid(),
            Tier = SubscriptionTier.Premium,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active,
            AutoRenew = true
        };
        
        _mockSubscriptionRepository.GetByIdAsync(subscriptionId, Arg.Any<CancellationToken>())
            .Returns(subscription);
        
        _mockSubscriptionRepository.UpdateAsync(Arg.Any<Core.Domain.Subscription>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<Core.Domain.Subscription>());
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "renew",
                ["subscriptionId"] = subscriptionId.ToString()
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        data!.Should().ContainKey("newEndDate");
        
        await _mockSubscriptionRepository.Received(1).UpdateAsync(
            Arg.Is<Core.Domain.Subscription>(s => s.EndDate > DateTime.UtcNow),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ExecuteAsync_WithInvalidAction_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "invalid_action"
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid action");
    }
}
