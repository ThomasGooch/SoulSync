using FluentAssertions;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;

namespace SoulSync.Core.Tests.Domain;

public class ConversationTests
{
    [Fact]
    public void Conversation_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        // Act
        var conversation = new Conversation
        {
            User1Id = user1Id,
            User2Id = user2Id,
            MatchId = matchId
        };

        // Assert
        conversation.Id.Should().NotBeEmpty();
        conversation.User1Id.Should().Be(user1Id);
        conversation.User2Id.Should().Be(user2Id);
        conversation.MatchId.Should().Be(matchId);
        conversation.Type.Should().Be(ConversationType.Initial);
        conversation.IsActive.Should().BeTrue();
        conversation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        conversation.MessageCount.Should().Be(0);
    }

    [Fact]
    public void Conversation_AddMessage_ShouldIncrementMessageCount()
    {
        // Arrange
        var conversation = new Conversation
        {
            User1Id = Guid.NewGuid(),
            User2Id = Guid.NewGuid(),
            MatchId = Guid.NewGuid()
        };

        // Act
        conversation.AddMessage();

        // Assert
        conversation.MessageCount.Should().Be(1);
        conversation.LastMessageAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Conversation_AddMessage_MultipleTimes_ShouldIncrementCorrectly()
    {
        // Arrange
        var conversation = new Conversation
        {
            User1Id = Guid.NewGuid(),
            User2Id = Guid.NewGuid(),
            MatchId = Guid.NewGuid()
        };

        // Act
        conversation.AddMessage();
        conversation.AddMessage();
        conversation.AddMessage();

        // Assert
        conversation.MessageCount.Should().Be(3);
    }

    [Fact]
    public void Conversation_UpdateType_ShouldChangeType()
    {
        // Arrange
        var conversation = new Conversation
        {
            User1Id = Guid.NewGuid(),
            User2Id = Guid.NewGuid(),
            MatchId = Guid.NewGuid()
        };

        // Act
        conversation.UpdateType(ConversationType.DatePlanning);

        // Assert
        conversation.Type.Should().Be(ConversationType.DatePlanning);
    }

    [Fact]
    public void Conversation_Archive_ShouldDeactivateConversation()
    {
        // Arrange
        var conversation = new Conversation
        {
            User1Id = Guid.NewGuid(),
            User2Id = Guid.NewGuid(),
            MatchId = Guid.NewGuid()
        };

        // Act
        conversation.Archive();

        // Assert
        conversation.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Conversation_IsUserInConversation_WithUser1Id_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation
        {
            User1Id = userId,
            User2Id = Guid.NewGuid(),
            MatchId = Guid.NewGuid()
        };

        // Act
        var result = conversation.IsUserInConversation(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Conversation_IsUserInConversation_WithUser2Id_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversation = new Conversation
        {
            User1Id = Guid.NewGuid(),
            User2Id = userId,
            MatchId = Guid.NewGuid()
        };

        // Act
        var result = conversation.IsUserInConversation(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Conversation_IsUserInConversation_WithUnrelatedUserId_ShouldReturnFalse()
    {
        // Arrange
        var conversation = new Conversation
        {
            User1Id = Guid.NewGuid(),
            User2Id = Guid.NewGuid(),
            MatchId = Guid.NewGuid()
        };
        var unrelatedUserId = Guid.NewGuid();

        // Act
        var result = conversation.IsUserInConversation(unrelatedUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Conversation_GetOtherUserId_WithUser1Id_ShouldReturnUser2Id()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var conversation = new Conversation
        {
            User1Id = user1Id,
            User2Id = user2Id,
            MatchId = Guid.NewGuid()
        };

        // Act
        var result = conversation.GetOtherUserId(user1Id);

        // Assert
        result.Should().Be(user2Id);
    }

    [Fact]
    public void Conversation_GetOtherUserId_WithUser2Id_ShouldReturnUser1Id()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var conversation = new Conversation
        {
            User1Id = user1Id,
            User2Id = user2Id,
            MatchId = Guid.NewGuid()
        };

        // Act
        var result = conversation.GetOtherUserId(user2Id);

        // Assert
        result.Should().Be(user1Id);
    }

    [Fact]
    public void Conversation_GetOtherUserId_WithUnrelatedUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var conversation = new Conversation
        {
            User1Id = Guid.NewGuid(),
            User2Id = Guid.NewGuid(),
            MatchId = Guid.NewGuid()
        };
        var unrelatedUserId = Guid.NewGuid();

        // Act & Assert
        var act = () => conversation.GetOtherUserId(unrelatedUserId);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*User is not part of this conversation*");
    }
}
