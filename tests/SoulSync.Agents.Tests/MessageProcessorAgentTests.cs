using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Communication;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Tests;

public class MessageProcessorAgentTests
{
    private readonly IMessageRepository _mockMessageRepository;
    private readonly IConversationRepository _mockConversationRepository;
    private readonly IUserRepository _mockUserRepository;
    private readonly ILogger<MessageProcessorAgent> _mockLogger;
    private readonly MessageProcessorAgent _agent;

    public MessageProcessorAgentTests()
    {
        _mockMessageRepository = Substitute.For<IMessageRepository>();
        _mockConversationRepository = Substitute.For<IConversationRepository>();
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockLogger = Substitute.For<ILogger<MessageProcessorAgent>>();
        
        _agent = new MessageProcessorAgent(
            _mockMessageRepository, 
            _mockConversationRepository, 
            _mockUserRepository,
            _mockLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidMessageData_ShouldProcessMessage()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var content = "Hello, how are you?";

        var sender = CreateTestUser(senderId, "John");
        var receiver = CreateTestUser(receiverId, "Jane");
        var conversation = CreateTestConversation(conversationId, senderId, receiverId);

        _mockUserRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(sender);
        _mockUserRepository.GetByIdAsync(receiverId, Arg.Any<CancellationToken>()).Returns(receiver);
        _mockConversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns(conversation);
        
        _mockMessageRepository.CreateAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<Message>());

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["senderId"] = senderId.ToString(),
                ["receiverId"] = receiverId.ToString(),
                ["conversationId"] = conversationId.ToString(),
                ["content"] = content
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("messageId");
        data.Should().ContainKey("status");
        data!["status"].Should().Be(MessageStatus.Sent);

        await _mockMessageRepository.Received(1).CreateAsync(
            Arg.Is<Message>(m => m.Content == content && m.SenderId == senderId),
            Arg.Any<CancellationToken>());
        
        await _mockConversationRepository.Received(1).UpdateAsync(
            Arg.Is<Conversation>(c => c.MessageCount > 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingSenderId_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["receiverId"] = Guid.NewGuid().ToString(),
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["content"] = "Test"
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("senderId");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentSender_ShouldReturnError()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        _mockUserRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["senderId"] = senderId.ToString(),
                ["receiverId"] = Guid.NewGuid().ToString(),
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["content"] = "Test"
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyContent_ShouldReturnError()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var sender = CreateTestUser(senderId, "John");
        var receiver = CreateTestUser(receiverId, "Jane");

        _mockUserRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(sender);
        _mockUserRepository.GetByIdAsync(receiverId, Arg.Any<CancellationToken>()).Returns(receiver);

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["senderId"] = senderId.ToString(),
                ["receiverId"] = receiverId.ToString(),
                ["conversationId"] = Guid.NewGuid().ToString(),
                ["content"] = ""
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentConversation_ShouldReturnError()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        
        var sender = CreateTestUser(senderId, "John");
        var receiver = CreateTestUser(receiverId, "Jane");

        _mockUserRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(sender);
        _mockUserRepository.GetByIdAsync(receiverId, Arg.Any<CancellationToken>()).Returns(receiver);
        _mockConversationRepository.GetByIdAsync(conversationId, Arg.Any<CancellationToken>()).Returns((Conversation?)null);

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["senderId"] = senderId.ToString(),
                ["receiverId"] = receiverId.ToString(),
                ["conversationId"] = conversationId.ToString(),
                ["content"] = "Test message"
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Conversation");
    }

    private User CreateTestUser(Guid id, string firstName)
    {
        return new User
        {
            FirstName = firstName,
            LastName = "Test",
            Email = $"{firstName.ToLower()}@test.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        };
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
}
