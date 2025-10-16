using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Matching;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Tests;

public class PreferenceLearningAgentTests
{
    private readonly IUserPreferencesRepository _mockPreferencesRepository;
    private readonly IMatchRepository _mockMatchRepository;
    private readonly IUserRepository _mockUserRepository;
    private readonly ILogger<PreferenceLearningAgent> _mockLogger;
    private readonly PreferenceLearningAgent _agent;

    public PreferenceLearningAgentTests()
    {
        _mockPreferencesRepository = Substitute.For<IUserPreferencesRepository>();
        _mockMatchRepository = Substitute.For<IMatchRepository>();
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockLogger = Substitute.For<ILogger<PreferenceLearningAgent>>();
        
        _agent = new PreferenceLearningAgent(
            _mockPreferencesRepository,
            _mockMatchRepository,
            _mockUserRepository,
            _mockLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidUserId_ShouldLearnPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences { UserId = userId };
        
        _mockPreferencesRepository.GetOrCreateAsync(userId, Arg.Any<CancellationToken>())
            .Returns(preferences);
        
        _mockMatchRepository.GetMatchesForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Match>
            {
                CreateTestMatch(userId, Guid.NewGuid(), 85, MatchStatus.Accepted),
                CreateTestMatch(userId, Guid.NewGuid(), 75, MatchStatus.Accepted),
                CreateTestMatch(userId, Guid.NewGuid(), 60, MatchStatus.Rejected)
            });

        _mockPreferencesRepository.UpdateAsync(Arg.Any<UserPreferences>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserPreferences>());

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        await _mockPreferencesRepository.Received(1).UpdateAsync(
            Arg.Is<UserPreferences>(p => p.LearningSessionCount > 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingUserId_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("userId");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoMatchHistory_ShouldStillSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences { UserId = userId };
        
        _mockPreferencesRepository.GetOrCreateAsync(userId, Arg.Any<CancellationToken>())
            .Returns(preferences);
        
        _mockMatchRepository.GetMatchesForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Match>());

        _mockPreferencesRepository.UpdateAsync(Arg.Any<UserPreferences>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserPreferences>());

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("preferencesUpdated");
    }

    [Fact]
    public async Task ExecuteAsync_WithAcceptedMatches_ShouldUpdateAverageScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences { UserId = userId };
        
        _mockPreferencesRepository.GetOrCreateAsync(userId, Arg.Any<CancellationToken>())
            .Returns(preferences);
        
        var user1 = CreateTestUser(Guid.NewGuid(), "hiking, cooking");
        var user2 = CreateTestUser(Guid.NewGuid(), "hiking, travel");
        
        _mockMatchRepository.GetMatchesForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Match>
            {
                CreateTestMatchWithUsers(userId, user1.Id, 85, MatchStatus.Accepted, user1),
                CreateTestMatchWithUsers(userId, user2.Id, 95, MatchStatus.Accepted, user2)
            });

        _mockUserRepository.GetByIdAsync(user1.Id, Arg.Any<CancellationToken>()).Returns(user1);
        _mockUserRepository.GetByIdAsync(user2.Id, Arg.Any<CancellationToken>()).Returns(user2);

        _mockPreferencesRepository.UpdateAsync(Arg.Any<UserPreferences>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserPreferences>());

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        await _mockPreferencesRepository.Received(1).UpdateAsync(
            Arg.Is<UserPreferences>(p => 
                p.MatchAcceptanceCount == 2 && 
                p.AverageAcceptedCompatibilityScore == 90),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedMatches_ShouldLearnInterestWeights()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences { UserId = userId };
        
        _mockPreferencesRepository.GetOrCreateAsync(userId, Arg.Any<CancellationToken>())
            .Returns(preferences);
        
        var acceptedUser = CreateTestUser(Guid.NewGuid(), "hiking, cooking, photography");
        var rejectedUser = CreateTestUser(Guid.NewGuid(), "gaming, movies");
        
        _mockMatchRepository.GetMatchesForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Match>
            {
                CreateTestMatchWithUsers(userId, acceptedUser.Id, 85, MatchStatus.Accepted, acceptedUser),
                CreateTestMatchWithUsers(userId, rejectedUser.Id, 60, MatchStatus.Rejected, rejectedUser)
            });

        _mockUserRepository.GetByIdAsync(acceptedUser.Id, Arg.Any<CancellationToken>()).Returns(acceptedUser);
        _mockUserRepository.GetByIdAsync(rejectedUser.Id, Arg.Any<CancellationToken>()).Returns(rejectedUser);

        _mockPreferencesRepository.UpdateAsync(Arg.Any<UserPreferences>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserPreferences>());

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        await _mockPreferencesRepository.Received(1).UpdateAsync(
            Arg.Is<UserPreferences>(p => p.InterestWeights.Count > 0),
            Arg.Any<CancellationToken>());
    }

    private static Match CreateTestMatch(Guid userId, Guid otherUserId, int score, MatchStatus status)
    {
        var match = new Match
        {
            UserId1 = userId,
            UserId2 = otherUserId,
            CompatibilityScore = score,
            Status = status
        };

        if (status == MatchStatus.Accepted)
            match.Accept();
        else if (status == MatchStatus.Rejected)
            match.Reject();

        return match;
    }

    private static Match CreateTestMatchWithUsers(
        Guid userId, 
        Guid otherUserId, 
        int score, 
        MatchStatus status, 
        User otherUser)
    {
        var match = CreateTestMatch(userId, otherUserId, score, status);
        match.User2 = otherUser;
        return match;
    }

    private static User CreateTestUser(Guid id, string interests)
    {
        return new User
        {
            Email = $"user{id}@example.com",
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
            Bio = "Test bio",
            Profile = new UserProfile
            {
                UserId = id,
                Interests = interests,
                Location = "San Francisco, CA",
                GenderIdentity = GenderIdentity.Male
            }
        };
    }
}
