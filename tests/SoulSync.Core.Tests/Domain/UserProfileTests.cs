using FluentAssertions;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;

namespace SoulSync.Core.Tests.Domain;

public class UserProfileTests
{
    [Fact]
    public void UserProfile_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var interests = "hiking, cooking, travel";
        var location = "San Francisco, CA";
        var occupation = "Software Engineer";

        // Act
        var profile = new UserProfile
        {
            UserId = userId,
            Interests = interests,
            Location = location,
            Occupation = occupation,
            GenderIdentity = GenderIdentity.Male,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Female }
        };

        // Assert
        profile.UserId.Should().Be(userId);
        profile.Interests.Should().Be(interests);
        profile.Location.Should().Be(location);
        profile.Occupation.Should().Be(occupation);
        profile.GenderIdentity.Should().Be(GenderIdentity.Male);
        profile.InterestedInGenders.Should().ContainSingle().Which.Should().Be(GenderIdentity.Female);
        profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void UserProfile_WhenAddingAIInsights_ShouldStoreInsightsCorrectly()
    {
        // Arrange
        var profile = new UserProfile
        {
            UserId = Guid.NewGuid(),
            Interests = "outdoor activities, cooking",
            GenderIdentity = GenderIdentity.Female,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Male }
        };

        var insights = "Profile shows strong outdoor enthusiasm and culinary passion. Personality traits suggest extroversion and creativity.";

        // Act
        profile.AddAIInsights(insights);

        // Assert
        profile.AIInsights.Should().Be(insights);
        profile.AIAnalysisCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        profile.HasAIAnalysis.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void UserProfile_WhenAddingEmptyAIInsights_ShouldThrowArgumentException(string insights)
    {
        // Arrange
        var profile = new UserProfile
        {
            UserId = Guid.NewGuid(),
            GenderIdentity = GenderIdentity.Female,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Male }
        };

        // Act & Assert
        var act = () => profile.AddAIInsights(insights);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*AI insights cannot be empty*");
    }

    [Fact]
    public void UserProfile_WhenUpdatingPreferences_ShouldUpdateCorrectly()
    {
        // Arrange
        var profile = new UserProfile
        {
            UserId = Guid.NewGuid(),
            GenderIdentity = GenderIdentity.Male,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Female }
        };

        var newInterests = "reading, music, art";
        var newLocation = "New York, NY";
        var newOccupation = "Designer";

        // Act
        profile.UpdatePreferences(newInterests, newLocation, newOccupation);

        // Assert
        profile.Interests.Should().Be(newInterests);
        profile.Location.Should().Be(newLocation);
        profile.Occupation.Should().Be(newOccupation);
        profile.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void UserProfile_WhenGettingInterestTags_ShouldReturnParsedTags()
    {
        // Arrange
        var profile = new UserProfile
        {
            UserId = Guid.NewGuid(),
            Interests = "hiking, cooking, travel, photography, music",
            GenderIdentity = GenderIdentity.Female,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Male }
        };

        // Act
        var tags = profile.InterestTags;

        // Assert
        tags.Should().HaveCount(5);
        tags.Should().Contain("hiking");
        tags.Should().Contain("cooking");
        tags.Should().Contain("travel");
        tags.Should().Contain("photography");
        tags.Should().Contain("music");
    }

    [Fact]
    public void UserProfile_WhenInterestsAreEmpty_InterestTagsShouldBeEmpty()
    {
        // Arrange
        var profile = new UserProfile
        {
            UserId = Guid.NewGuid(),
            Interests = "",
            GenderIdentity = GenderIdentity.Female,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Male }
        };

        // Act
        var tags = profile.InterestTags;

        // Assert
        tags.Should().BeEmpty();
    }

    [Fact]
    public void UserProfile_WhenCalculatingCompatibilityScore_ShouldReturnValidScore()
    {
        // Arrange
        var profile1 = new UserProfile
        {
            UserId = Guid.NewGuid(),
            Interests = "hiking, cooking, travel",
            GenderIdentity = GenderIdentity.Male,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Female }
        };

        var profile2 = new UserProfile
        {
            UserId = Guid.NewGuid(),
            Interests = "outdoor activities, culinary arts, adventures",
            GenderIdentity = GenderIdentity.Female,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Male }
        };

        // Act
        var score = profile1.CalculateBasicCompatibilityScore(profile2);

        // Assert
        score.Should().BeInRange(0, 100);
        score.Should().BeGreaterThan(0); // Should have some compatibility due to similar interests
    }
}