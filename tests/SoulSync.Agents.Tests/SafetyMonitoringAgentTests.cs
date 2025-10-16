using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Communication;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Tests;

public class SafetyMonitoringAgentTests
{
    private readonly IMessageRepository _mockMessageRepository;
    private readonly ISafetyFlagRepository _mockSafetyFlagRepository;
    private readonly IAIService _mockAIService;
    private readonly ILogger<SafetyMonitoringAgent> _mockLogger;
    private readonly SafetyMonitoringAgent _agent;

    public SafetyMonitoringAgentTests()
    {
        _mockMessageRepository = Substitute.For<IMessageRepository>();
        _mockSafetyFlagRepository = Substitute.For<ISafetyFlagRepository>();
        _mockAIService = Substitute.For<IAIService>();
        _mockLogger = Substitute.For<ILogger<SafetyMonitoringAgent>>();
        
        _agent = new SafetyMonitoringAgent(
            _mockMessageRepository, 
            _mockSafetyFlagRepository, 
            _mockAIService,
            _mockLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithSafeMessage_ShouldReturnSafeStatus()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = CreateTestMessage(messageId, "Hello, how are you today?");

        _mockMessageRepository.GetByIdAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);
        _mockAIService.AnalyzeSafetyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>
            {
                ["safetyLevel"] = "Safe",
                ["score"] = 95,
                ["issues"] = new List<string>()
            });

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["messageId"] = messageId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("safetyLevel");
        data!["safetyLevel"].Should().Be(SafetyLevel.Safe);
        data.Should().ContainKey("requiresAction");
        data["requiresAction"].Should().Be(false);
    }

    [Fact]
    public async Task ExecuteAsync_WithInappropriateContent_ShouldFlagMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = CreateTestMessage(messageId, "Inappropriate content here");

        _mockMessageRepository.GetByIdAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);
        _mockAIService.AnalyzeSafetyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>
            {
                ["safetyLevel"] = "Warning",
                ["score"] = 40,
                ["issues"] = new List<string> { "Inappropriate language" }
            });

        _mockSafetyFlagRepository.CreateAsync(Arg.Any<SafetyFlag>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<SafetyFlag>());

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["messageId"] = messageId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var data = result.Data as Dictionary<string, object>;
        data!["safetyLevel"].Should().Be(SafetyLevel.Warning);
        data["requiresAction"].Should().Be(true);

        await _mockSafetyFlagRepository.Received(1).CreateAsync(
            Arg.Is<SafetyFlag>(f => f.MessageId == messageId && f.Level == SafetyLevel.Warning),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithDangerousContent_ShouldBlockMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = CreateTestMessage(messageId, "Dangerous content");

        _mockMessageRepository.GetByIdAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);
        _mockAIService.AnalyzeSafetyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>
            {
                ["safetyLevel"] = "Dangerous",
                ["score"] = 10,
                ["issues"] = new List<string> { "Harassment", "Threats" }
            });

        _mockSafetyFlagRepository.CreateAsync(Arg.Any<SafetyFlag>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<SafetyFlag>());

        _mockMessageRepository.UpdateAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<Message>());

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["messageId"] = messageId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data!["safetyLevel"].Should().Be(SafetyLevel.Dangerous);
        data["requiresAction"].Should().Be(true);
        
        // Verify the message was updated and flagged
        await _mockMessageRepository.Received(1).UpdateAsync(
            Arg.Any<Message>(),
            Arg.Any<CancellationToken>());
        
        message.IsFlagged.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingMessageId_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("messageId");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentMessage_ShouldReturnError()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        _mockMessageRepository.GetByIdAsync(messageId, Arg.Any<CancellationToken>()).Returns((Message?)null);

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["messageId"] = messageId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAIServiceFails_ShouldUseFallbackAnalysis()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = CreateTestMessage(messageId, "Test message");

        _mockMessageRepository.GetByIdAsync(messageId, Arg.Any<CancellationToken>()).Returns(message);
        _mockAIService.AnalyzeSafetyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Dictionary<string, object>>(new Exception("AI service unavailable")));

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["messageId"] = messageId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("safetyLevel");
        data.Should().ContainKey("usedFallback");
        data!["usedFallback"].Should().Be(true);
    }

    private Message CreateTestMessage(Guid id, string content)
    {
        return new Message
        {
            Id = id,
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Content = content
        };
    }
}
