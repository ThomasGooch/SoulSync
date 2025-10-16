using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;
using SoulSync.Web.Components.Pages;

namespace SoulSync.Web.Tests.Components.Pages;

public class ChatTests : TestContext
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _conversationId = Guid.NewGuid();

    public ChatTests()
    {
        _messageRepository = Substitute.For<IMessageRepository>();
        _conversationRepository = Substitute.For<IConversationRepository>();
        Services.AddSingleton(_messageRepository);
        Services.AddSingleton(_conversationRepository);
    }

    [Fact]
    public void Chat_ShouldRender_WithMessagesList()
    {
        // Arrange
        var messages = CreateTestMessages();
        _messageRepository.GetMessagesByConversationIdAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(messages);

        // Act
        var cut = RenderComponent<Chat>(parameters => parameters
            .Add(p => p.ConversationId, _conversationId.ToString())
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        // Assert
        cut.Markup.Should().Contain("Chat");
    }

    [Fact]
    public void Chat_ShouldDisplay_MessageInputField()
    {
        // Arrange
        var messages = CreateTestMessages();
        _messageRepository.GetMessagesByConversationIdAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(messages);

        // Act
        var cut = RenderComponent<Chat>(parameters => parameters
            .Add(p => p.ConversationId, _conversationId.ToString())
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        // Assert
        cut.Find("input[type='text']").Should().NotBeNull();
        cut.Find("button[type='submit']").Should().NotBeNull();
    }

    [Fact]
    public void Chat_ShouldDisplay_PreviousMessages()
    {
        // Arrange
        var messages = CreateTestMessages();
        _messageRepository.GetMessagesByConversationIdAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(messages);

        // Act
        var cut = RenderComponent<Chat>(parameters => parameters
            .Add(p => p.ConversationId, _conversationId.ToString())
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        // Assert
        cut.Markup.Should().Contain("Hello there!");
        cut.Markup.Should().Contain("Hi! How are you?");
    }

    [Fact]
    public async Task Chat_WhenMessageSent_ShouldCallMessageRepository()
    {
        // Arrange
        var messages = CreateTestMessages();
        var otherUserId = Guid.NewGuid();
        
        _messageRepository.GetMessagesByConversationIdAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(messages);

        var conversation = new Conversation
        {
            User1Id = _currentUserId,
            User2Id = otherUserId
        };
        _conversationRepository.GetByIdAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(conversation);

        _messageRepository.CreateAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<Message>(0));

        var cut = RenderComponent<Chat>(parameters => parameters
            .Add(p => p.ConversationId, _conversationId.ToString())
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        var input = cut.Find("input[type='text']");
        var form = cut.Find("form");

        // Act
        input.Change("Test message");
        await form.SubmitAsync();

        // Assert
        await _messageRepository.Received(1).CreateAsync(
            Arg.Is<Message>(m => m.Content == "Test message" && m.SenderId == _currentUserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Chat_WhenNoMessages_ShouldDisplayEmptyState()
    {
        // Arrange
        _messageRepository.GetMessagesByConversationIdAsync(_conversationId, Arg.Any<CancellationToken>())
            .Returns(new List<Message>());

        // Act
        var cut = RenderComponent<Chat>(parameters => parameters
            .Add(p => p.ConversationId, _conversationId.ToString())
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("No messages") || cut.Markup.Contains("Start a conversation"),
            timeout: TimeSpan.FromSeconds(2));
    }

    private IEnumerable<Message> CreateTestMessages()
    {
        var otherUserId = Guid.NewGuid();
        return new List<Message>
        {
            new Message
            {
                SenderId = _currentUserId,
                ReceiverId = otherUserId,
                ConversationId = _conversationId,
                Content = "Hello there!",
                Status = MessageStatus.Delivered,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Message
            {
                SenderId = otherUserId,
                ReceiverId = _currentUserId,
                ConversationId = _conversationId,
                Content = "Hi! How are you?",
                Status = MessageStatus.Read,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };
    }
}
