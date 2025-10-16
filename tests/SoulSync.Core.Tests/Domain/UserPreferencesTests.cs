using FluentAssertions;
using SoulSync.Core.Domain;

namespace SoulSync.Core.Tests.Domain;

public class UserPreferencesTests
{
    [Fact]
    public void UserPreferences_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var preferences = new UserPreferences();

        // Assert
        preferences.Id.Should().NotBe(Guid.Empty);
        preferences.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        preferences.ProfileViewCount.Should().Be(0);
        preferences.MatchAcceptanceCount.Should().Be(0);
        preferences.MatchRejectionCount.Should().Be(0);
        preferences.LearningSessionCount.Should().Be(0);
    }

    [Fact]
    public void RecordMatchAcceptance_ShouldIncrementCountAndUpdateAverage()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act
        preferences.RecordMatchAcceptance(80);
        preferences.RecordMatchAcceptance(90);

        // Assert
        preferences.MatchAcceptanceCount.Should().Be(2);
        preferences.AverageAcceptedCompatibilityScore.Should().Be(85);
    }

    [Fact]
    public void RecordMatchRejection_ShouldIncrementCount()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act
        preferences.RecordMatchRejection();
        preferences.RecordMatchRejection();

        // Assert
        preferences.MatchRejectionCount.Should().Be(2);
    }

    [Fact]
    public void RecordProfileView_ShouldIncrementCount()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act
        preferences.RecordProfileView();
        preferences.RecordProfileView();
        preferences.RecordProfileView();

        // Assert
        preferences.ProfileViewCount.Should().Be(3);
    }

    [Fact]
    public void UpdateInterestWeight_ShouldAddOrUpdateWeight()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act
        preferences.UpdateInterestWeight("Hiking", 0.8);
        preferences.UpdateInterestWeight("Cooking", 0.6);

        // Assert
        preferences.InterestWeights.Should().ContainKey("hiking");
        preferences.InterestWeights["hiking"].Should().Be(0.8);
        preferences.InterestWeights.Should().ContainKey("cooking");
        preferences.InterestWeights["cooking"].Should().Be(0.6);
    }

    [Fact]
    public void UpdateInterestWeight_WithInvalidWeight_ShouldThrowException()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => preferences.UpdateInterestWeight("Hiking", -0.5));
        Assert.Throws<ArgumentException>(() => preferences.UpdateInterestWeight("Hiking", 1.5));
    }

    [Fact]
    public void UpdatePersonalityTraitPreference_ShouldAddOrUpdatePreference()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act
        preferences.UpdatePersonalityTraitPreference("Extroverted", 0.7);
        preferences.UpdatePersonalityTraitPreference("Analytical", -0.3);

        // Assert
        preferences.PersonalityTraitPreferences.Should().ContainKey("extroverted");
        preferences.PersonalityTraitPreferences["extroverted"].Should().Be(0.7);
        preferences.PersonalityTraitPreferences.Should().ContainKey("analytical");
        preferences.PersonalityTraitPreferences["analytical"].Should().Be(-0.3);
    }

    [Fact]
    public void UpdatePersonalityTraitPreference_WithInvalidPreference_ShouldThrowException()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => preferences.UpdatePersonalityTraitPreference("Trait", -1.5));
        Assert.Throws<ArgumentException>(() => preferences.UpdatePersonalityTraitPreference("Trait", 1.5));
    }

    [Fact]
    public void RecordLearningSession_ShouldIncrementCountAndUpdateTimestamp()
    {
        // Arrange
        var preferences = new UserPreferences();
        var beforeSession = DateTime.UtcNow;

        // Act
        preferences.RecordLearningSession();

        // Assert
        preferences.LearningSessionCount.Should().Be(1);
        preferences.LastLearningSessionAt.Should().NotBeNull();
        preferences.LastLearningSessionAt.Should().BeOnOrAfter(beforeSession);
    }

    [Fact]
    public void GetAcceptanceRate_WithNoMatches_ShouldReturnZero()
    {
        // Arrange
        var preferences = new UserPreferences();

        // Act
        var rate = preferences.GetAcceptanceRate();

        // Assert
        rate.Should().Be(0);
    }

    [Fact]
    public void GetAcceptanceRate_WithMixedMatches_ShouldCalculateCorrectly()
    {
        // Arrange
        var preferences = new UserPreferences();
        preferences.RecordMatchAcceptance(80);
        preferences.RecordMatchAcceptance(90);
        preferences.RecordMatchRejection();
        preferences.RecordMatchRejection();

        // Act
        var rate = preferences.GetAcceptanceRate();

        // Assert
        rate.Should().Be(0.5); // 2 accepted out of 4 total
    }
}
