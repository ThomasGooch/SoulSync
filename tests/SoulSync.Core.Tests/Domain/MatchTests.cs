using FluentAssertions;
using SoulSync.Core.Domain;

namespace SoulSync.Core.Tests.Domain;

public class MatchTests
{
    [Fact]
    public void Match_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var match = new Match();

        // Assert
        match.Id.Should().NotBe(Guid.Empty);
        match.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        match.Status.Should().Be(MatchStatus.Pending);
        match.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Match_WithUserIds_ShouldSetProperties()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var compatibilityScore = 85;

        // Act
        var match = new Match
        {
            UserId1 = userId1,
            UserId2 = userId2,
            CompatibilityScore = compatibilityScore
        };

        // Assert
        match.UserId1.Should().Be(userId1);
        match.UserId2.Should().Be(userId2);
        match.CompatibilityScore.Should().Be(compatibilityScore);
    }

    [Fact]
    public void Accept_ShouldUpdateStatusAndTimestamp()
    {
        // Arrange
        var match = new Match();
        var beforeAccept = DateTime.UtcNow;

        // Act
        match.Accept();

        // Assert
        match.Status.Should().Be(MatchStatus.Accepted);
        match.AcceptedAt.Should().NotBeNull();
        match.AcceptedAt.Should().BeOnOrAfter(beforeAccept);
        match.LastModifiedAt.Should().BeOnOrAfter(beforeAccept);
    }

    [Fact]
    public void Reject_ShouldUpdateStatusAndDeactivate()
    {
        // Arrange
        var match = new Match();

        // Act
        match.Reject();

        // Assert
        match.Status.Should().Be(MatchStatus.Rejected);
        match.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateCompatibilityScore_ShouldSetNewScore()
    {
        // Arrange
        var match = new Match { CompatibilityScore = 70 };

        // Act
        match.UpdateCompatibilityScore(90);

        // Assert
        match.CompatibilityScore.Should().Be(90);
    }

    [Fact]
    public void UpdateCompatibilityScore_WithInvalidScore_ShouldThrowException()
    {
        // Arrange
        var match = new Match();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => match.UpdateCompatibilityScore(-10));
        Assert.Throws<ArgumentException>(() => match.UpdateCompatibilityScore(150));
    }

    [Fact]
    public void IsUserInMatch_WithMatchedUser_ShouldReturnTrue()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var match = new Match { UserId1 = userId1, UserId2 = userId2 };

        // Act & Assert
        match.IsUserInMatch(userId1).Should().BeTrue();
        match.IsUserInMatch(userId2).Should().BeTrue();
    }

    [Fact]
    public void IsUserInMatch_WithNonMatchedUser_ShouldReturnFalse()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var match = new Match { UserId1 = userId1, UserId2 = userId2 };

        // Act & Assert
        match.IsUserInMatch(userId3).Should().BeFalse();
    }
}
