using FluentAssertions;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;

namespace SoulSync.Core.Tests.Domain;

public class SafetyFlagTests
{
    [Fact]
    public void SafetyFlag_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var flaggedByUserId = Guid.NewGuid();
        var reason = "Inappropriate content";

        // Act
        var flag = new SafetyFlag
        {
            MessageId = messageId,
            ConversationId = conversationId,
            FlaggedByUserId = flaggedByUserId,
            Reason = reason
        };

        // Assert
        flag.Id.Should().NotBeEmpty();
        flag.MessageId.Should().Be(messageId);
        flag.ConversationId.Should().Be(conversationId);
        flag.FlaggedByUserId.Should().Be(flaggedByUserId);
        flag.Reason.Should().Be(reason);
        flag.Level.Should().Be(SafetyLevel.Suspicious);
        flag.IsResolved.Should().BeFalse();
        flag.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("", "Reason cannot be empty")]
    [InlineData(null, "Reason cannot be empty")]
    [InlineData("   ", "Reason cannot be empty")]
    public void SafetyFlag_WhenCreatedWithInvalidReason_ShouldThrowArgumentException(string reason, string expectedMessage)
    {
        // Act & Assert
        var act = () => new SafetyFlag
        {
            MessageId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            FlaggedByUserId = Guid.NewGuid(),
            Reason = reason
        };

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public void SafetyFlag_UpdateLevel_ShouldChangeSafetyLevel()
    {
        // Arrange
        var flag = new SafetyFlag
        {
            MessageId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            FlaggedByUserId = Guid.NewGuid(),
            Reason = "Test reason"
        };

        // Act
        flag.UpdateLevel(SafetyLevel.Dangerous);

        // Assert
        flag.Level.Should().Be(SafetyLevel.Dangerous);
    }

    [Fact]
    public void SafetyFlag_Resolve_WithoutAction_ShouldMarkAsResolved()
    {
        // Arrange
        var flag = new SafetyFlag
        {
            MessageId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            FlaggedByUserId = Guid.NewGuid(),
            Reason = "Test reason"
        };

        // Act
        flag.Resolve();

        // Assert
        flag.IsResolved.Should().BeTrue();
        flag.ResolvedAt.Should().NotBeNull();
        flag.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        flag.ActionTaken.Should().BeNull();
    }

    [Fact]
    public void SafetyFlag_Resolve_WithAction_ShouldMarkAsResolvedWithAction()
    {
        // Arrange
        var flag = new SafetyFlag
        {
            MessageId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            FlaggedByUserId = Guid.NewGuid(),
            Reason = "Test reason"
        };
        var action = "User warned";

        // Act
        flag.Resolve(action);

        // Assert
        flag.IsResolved.Should().BeTrue();
        flag.ResolvedAt.Should().NotBeNull();
        flag.ActionTaken.Should().Be(action);
    }

    [Fact]
    public void SafetyFlag_Escalate_ShouldIncreaseSafetyLevel()
    {
        // Arrange
        var flag = new SafetyFlag
        {
            MessageId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            FlaggedByUserId = Guid.NewGuid(),
            Reason = "Test reason"
        };

        // Act
        flag.Escalate();

        // Assert
        flag.Level.Should().Be(SafetyLevel.Warning);
    }

    [Fact]
    public void SafetyFlag_Escalate_WhenAtDangerous_ShouldMoveToBlocked()
    {
        // Arrange
        var flag = new SafetyFlag
        {
            MessageId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            FlaggedByUserId = Guid.NewGuid(),
            Reason = "Test reason"
        };
        flag.UpdateLevel(SafetyLevel.Dangerous);

        // Act
        flag.Escalate();

        // Assert
        flag.Level.Should().Be(SafetyLevel.Blocked);
    }

    [Fact]
    public void SafetyFlag_Escalate_WhenAtBlocked_ShouldRemainBlocked()
    {
        // Arrange
        var flag = new SafetyFlag
        {
            MessageId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            FlaggedByUserId = Guid.NewGuid(),
            Reason = "Test reason"
        };
        flag.UpdateLevel(SafetyLevel.Blocked);

        // Act
        flag.Escalate();

        // Assert
        flag.Level.Should().Be(SafetyLevel.Blocked);
    }
}
