using FluentAssertions;
using SoulSync.Core.Domain;

namespace SoulSync.Core.Tests.Domain;

public class CompatibilityScoreTests
{
    [Fact]
    public void CompatibilityScore_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var score = new CompatibilityScore();

        // Assert
        score.Id.Should().NotBe(Guid.Empty);
        score.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        score.OverallScore.Should().Be(0);
    }

    [Fact]
    public void CompatibilityScore_WithFactors_ShouldCalculateOverallScore()
    {
        // Arrange & Act
        var score = new CompatibilityScore
        {
            InterestCompatibility = 80,
            PersonalityCompatibility = 90,
            LifestyleCompatibility = 70,
            ValueCompatibility = 85
        };

        // Assert
        // Overall score should be weighted average
        score.OverallScore.Should().BeInRange(75, 85);
    }

    [Fact]
    public void AddFactorScore_ShouldAddScoreToFactors()
    {
        // Arrange
        var score = new CompatibilityScore();

        // Act
        score.AddFactorScore("CustomFactor", 75);

        // Assert
        score.FactorScores.Should().ContainKey("CustomFactor");
        score.FactorScores["CustomFactor"].Should().Be(75);
    }

    [Fact]
    public void AddFactorScore_WithInvalidScore_ShouldThrowException()
    {
        // Arrange
        var score = new CompatibilityScore();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => score.AddFactorScore("Factor", -5));
        Assert.Throws<ArgumentException>(() => score.AddFactorScore("Factor", 105));
    }

    [Fact]
    public void GetCompatibilityLevel_WithHighScore_ShouldReturnExcellent()
    {
        // Arrange
        var score = new CompatibilityScore
        {
            InterestCompatibility = 90,
            PersonalityCompatibility = 95,
            LifestyleCompatibility = 88,
            ValueCompatibility = 92
        };

        // Act
        var level = score.GetCompatibilityLevel();

        // Assert
        level.Should().Be("Excellent");
    }

    [Fact]
    public void GetCompatibilityLevel_WithMediumScore_ShouldReturnGood()
    {
        // Arrange
        var score = new CompatibilityScore
        {
            InterestCompatibility = 70,
            PersonalityCompatibility = 75,
            LifestyleCompatibility = 68,
            ValueCompatibility = 72
        };

        // Act
        var level = score.GetCompatibilityLevel();

        // Assert
        level.Should().Be("Good");
    }

    [Fact]
    public void GetCompatibilityLevel_WithLowScore_ShouldReturnFair()
    {
        // Arrange
        var score = new CompatibilityScore
        {
            InterestCompatibility = 40,
            PersonalityCompatibility = 45,
            LifestyleCompatibility = 42,
            ValueCompatibility = 38
        };

        // Act
        var level = score.GetCompatibilityLevel();

        // Assert
        level.Should().Be("Fair");
    }
}
