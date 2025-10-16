using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;
using SoulSync.Web.Components.Pages;

namespace SoulSync.Web.Tests.Components.Pages;

public class MatchesTests : TestContext
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUserRepository _userRepository;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public MatchesTests()
    {
        _matchRepository = Substitute.For<IMatchRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        Services.AddSingleton(_matchRepository);
        Services.AddSingleton(_userRepository);
    }

    [Fact]
    public void Matches_ShouldRender_WithMatchesList()
    {
        // Arrange
        var matches = CreateTestMatches();
        _matchRepository.GetMatchesForUserAsync(_currentUserId, Arg.Any<CancellationToken>())
            .Returns(matches);

        // Act
        var cut = RenderComponent<Matches>(parameters => parameters
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        // Assert
        cut.Markup.Should().Contain("Matches");
    }

    [Fact]
    public void Matches_ShouldDisplay_MatchCards()
    {
        // Arrange
        var matches = CreateTestMatches();
        _matchRepository.GetMatchesForUserAsync(_currentUserId, Arg.Any<CancellationToken>())
            .Returns(matches);

        // Act
        var cut = RenderComponent<Matches>(parameters => parameters
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        // Assert
        cut.Markup.Should().Contain("card");
    }

    [Fact]
    public void Matches_ShouldDisplay_CompatibilityScore()
    {
        // Arrange
        var matches = CreateTestMatches();
        _matchRepository.GetMatchesForUserAsync(_currentUserId, Arg.Any<CancellationToken>())
            .Returns(matches);

        // Act
        var cut = RenderComponent<Matches>(parameters => parameters
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        // Assert
        cut.Markup.Should().Contain("85");
    }

    [Fact]
    public void Matches_WhenNoMatches_ShouldDisplayEmptyMessage()
    {
        // Arrange
        _matchRepository.GetMatchesForUserAsync(_currentUserId, Arg.Any<CancellationToken>())
            .Returns(new List<Match>());

        // Act
        var cut = RenderComponent<Matches>(parameters => parameters
            .Add(p => p.CurrentUserId, _currentUserId.ToString()));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("No matches") || cut.Markup.Contains("no matches"),
            timeout: TimeSpan.FromSeconds(2));
    }

    private IEnumerable<Match> CreateTestMatches()
    {
        return new List<Match>
        {
            new Match
            {
                UserId1 = _currentUserId,
                UserId2 = Guid.NewGuid(),
                CompatibilityScore = 85,
                Status = MatchStatus.Pending,
                User2 = new User
                {
                    Email = "match1@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-26)),
                    Bio = "Love hiking and coffee",
                    Profile = new UserProfile
                    {
                        Interests = "hiking,coffee,reading",
                        Location = "San Francisco, CA",
                        GenderIdentity = SoulSync.Core.Enums.GenderIdentity.Female
                    }
                }
            },
            new Match
            {
                UserId1 = _currentUserId,
                UserId2 = Guid.NewGuid(),
                CompatibilityScore = 78,
                Status = MatchStatus.Pending,
                User2 = new User
                {
                    Email = "match2@example.com",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-24)),
                    Bio = "Foodie and traveler",
                    Profile = new UserProfile
                    {
                        Interests = "food,travel,yoga",
                        Location = "San Francisco, CA",
                        GenderIdentity = SoulSync.Core.Enums.GenderIdentity.Female
                    }
                }
            }
        };
    }
}
