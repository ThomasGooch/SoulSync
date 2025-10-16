using FluentAssertions;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;

namespace SoulSync.Core.Tests.Domain;

public class MessageTests
{
    [Fact]
    public void Message_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var content = "Hello, how are you?";
        var conversationId = Guid.NewGuid();

        // Act
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            ConversationId = conversationId
        };

        // Assert
        message.Id.Should().NotBeEmpty();
        message.SenderId.Should().Be(senderId);
        message.ReceiverId.Should().Be(receiverId);
        message.Content.Should().Be(content);
        message.ConversationId.Should().Be(conversationId);
        message.Status.Should().Be(MessageStatus.Sent);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.IsFlagged.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "Content cannot be empty")]
    [InlineData(null, "Content cannot be empty")]
    [InlineData("   ", "Content cannot be empty")]
    public void Message_WhenCreatedWithInvalidContent_ShouldThrowArgumentException(string content, string expectedMessage)
    {
        // Act & Assert
        var act = () => new Message
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            Content = content,
            ConversationId = Guid.NewGuid()
        };

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public void Message_MarkAsRead_ShouldUpdateStatusAndReadAt()
    {
        // Arrange
        var message = new Message
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            Content = "Test message",
            ConversationId = Guid.NewGuid()
        };

        // Act
        message.MarkAsRead();

        // Assert
        message.Status.Should().Be(MessageStatus.Read);
        message.ReadAt.Should().NotBeNull();
        message.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Message_MarkAsDelivered_ShouldUpdateStatusAndDeliveredAt()
    {
        // Arrange
        var message = new Message
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            Content = "Test message",
            ConversationId = Guid.NewGuid()
        };

        // Act
        message.MarkAsDelivered();

        // Assert
        message.Status.Should().Be(MessageStatus.Delivered);
        message.DeliveredAt.Should().NotBeNull();
        message.DeliveredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Message_Flag_ShouldUpdateFlagStatusAndReason()
    {
        // Arrange
        var message = new Message
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            Content = "Test message",
            ConversationId = Guid.NewGuid()
        };
        var reason = "Inappropriate content";

        // Act
        message.Flag(reason);

        // Assert
        message.IsFlagged.Should().BeTrue();
        message.FlaggedReason.Should().Be(reason);
        message.FlaggedAt.Should().NotBeNull();
        message.FlaggedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.Status.Should().Be(MessageStatus.Flagged);
    }

    [Fact]
    public void Message_Flag_WithEmptyReason_ShouldThrowArgumentException()
    {
        // Arrange
        var message = new Message
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            Content = "Test message",
            ConversationId = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => message.Flag("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Flag reason cannot be empty*");
    }

    [Fact]
    public void Message_IsFromUser_WithSenderId_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var message = new Message
        {
            SenderId = userId,
            ReceiverId = Guid.NewGuid(),
            Content = "Test message",
            ConversationId = Guid.NewGuid()
        };

        // Act
        var result = message.IsFromUser(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Message_IsFromUser_WithReceiverId_ShouldReturnFalse()
    {
        // Arrange
        var message = new Message
        {
            SenderId = Guid.NewGuid(),
            ReceiverId = Guid.NewGuid(),
            Content = "Test message",
            ConversationId = Guid.NewGuid()
        };

        // Act
        var result = message.IsFromUser(message.ReceiverId);

        // Assert
        result.Should().BeFalse();
    }
}
