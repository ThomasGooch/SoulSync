using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Communication;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Tests;

public class ConversationCoachAgentTests
{
    private readonly IConversationRepository _mockConversationRepository;
    private readonly IMessageRepository _mockMessageRepository;
    private readonly IAIService _mockAIService;
    private readonly ILogger<ConversationCoachAgent> _mockLogger;
    private readonly ConversationCoachAgent _agent;

    public ConversationCoachAgentTests()
    {
        _mockConversationRepository = Substitute.For<IConversationRepository>();
        _mockMessageRepository = Substitute.For<IMessageRepository>();
        _mockAIService = Substitute.For<IAIService>();
        _mockLogger = Substitute.For<ILogger<ConversationCoachAgent>>();
        
        _agent = new ConversationCoachAgent(
            _mockConversationRepository, 
            _mockMessageRepository, 
            _mockAIService,
            _mockLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidConversation_ShouldProvideCoaching()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversation = CreateTestConversation(conversationId, userId, Guid.NewGuid());
        var messages = CreateTestMessages(conversationId, 5);

        _mockConversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(conversation);
        _mockMessageRepository.GetMessagesByConversationIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(messages);
        
        _mockAIService.AnalyzeConversationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>
            {
                ["engagement"] = "High",
                ["sentiment"] = "Positive",
                ["suggestions"] = new List<string> { "Ask about their interests", "Share a personal story" }
            });

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["conversationId"] = conversationId.ToString(),
                ["userId"] = userId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("conversationHealth");
        data.Should().ContainKey("suggestions");
        data.Should().ContainKey("sentiment");
        
        var suggestions = data!["suggestions"] as List<string>;
        suggestions.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithLowEngagement_ShouldProvideEngagementTips()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversation = CreateTestConversation(conversationId, userId, Guid.NewGuid());
        var messages = CreateTestMessages(conversationId, 2);

        _mockConversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(conversation);
        _mockMessageRepository.GetMessagesByConversationIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(messages);
        
        _mockAIService.AnalyzeConversationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, object>
            {
                ["engagement"] = "Low",
                ["sentiment"] = "Neutral",
                ["suggestions"] = new List<string> { "Try asking open-ended questions", "Show interest in their hobbies" }
            });

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["conversationId"] = conversationId.ToString(),
                ["userId"] = userId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data!["conversationHealth"].Should().Be("NeedsAttention");
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingConversationId_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = Guid.NewGuid().ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("conversationId");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentConversation_ShouldReturnError()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _mockConversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns((Conversation?)null);

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["conversationId"] = conversationId.ToString(),
                ["userId"] = Guid.NewGuid().ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoMessages_ShouldProvideInitialTips()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversation = CreateTestConversation(conversationId, userId, Guid.NewGuid());

        _mockConversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(conversation);
        _mockMessageRepository.GetMessagesByConversationIdAsync(conversationId, Arg.Any<CancellationToken>())
            .Returns(new List<Message>());

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["conversationId"] = conversationId.ToString(),
                ["userId"] = userId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("suggestions");
        data!["conversationHealth"].Should().Be("Initial");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAIServiceFails_ShouldUseFallbackSuggestions()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var conversation = CreateTestConversation(conversationId, userId, Guid.NewGuid());
        var messages = CreateTestMessages(conversationId, 3);

        _mockConversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(conversation);
        _mockMessageRepository.GetMessagesByConversationIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(messages);
        
        _mockAIService.AnalyzeConversationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Dictionary<string, object>>(new Exception("AI service unavailable")));

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["conversationId"] = conversationId.ToString(),
                ["userId"] = userId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("usedFallback");
        data!["usedFallback"].Should().Be(true);
        data.Should().ContainKey("suggestions");
    }

    private Conversation CreateTestConversation(Guid id, Guid user1Id, Guid user2Id)
    {
        return new Conversation
        {
            User1Id = user1Id,
            User2Id = user2Id,
            MatchId = Guid.NewGuid()
        };
    }

    private List<Message> CreateTestMessages(Guid conversationId, int count)
    {
        var messages = new List<Message>();
        for (int i = 0; i < count; i++)
        {
            messages.Add(new Message
            {
                SenderId = Guid.NewGuid(),
                ReceiverId = Guid.NewGuid(),
                ConversationId = conversationId,
                Content = $"Test message {i + 1}"
            });
        }
        return messages;
    }
}
