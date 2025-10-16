using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Matching;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Tests;

public class MatchRankingAgentTests
{
    private readonly IUserRepository _mockUserRepository;
    private readonly IUserPreferencesRepository _mockPreferencesRepository;
    private readonly CompatibilityAgent _mockCompatibilityAgent;
    private readonly ILogger<MatchRankingAgent> _mockLogger;
    private readonly MatchRankingAgent _agent;

    public MatchRankingAgentTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockPreferencesRepository = Substitute.For<IUserPreferencesRepository>();
        _mockCompatibilityAgent = Substitute.For<CompatibilityAgent>(
            _mockUserRepository,
            Substitute.For<IAIService>(),
            Substitute.For<ILogger<CompatibilityAgent>>());
        _mockLogger = Substitute.For<ILogger<MatchRankingAgent>>();
        
        _agent = new MatchRankingAgent(
            _mockUserRepository,
            _mockPreferencesRepository,
            _mockCompatibilityAgent,
            _mockLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidUserId_ShouldReturnRankedMatches()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUser = CreateTestUser(userId, "John", "hiking, cooking");
        var candidate1 = CreateTestUser(Guid.NewGuid(), "Jane", "hiking, photography");
        var candidate2 = CreateTestUser(Guid.NewGuid(), "Sarah", "cooking, travel");
        
        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(currentUser);
        _mockUserRepository.GetPotentialMatchesAsync(userId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { candidate1, candidate2 });

        _mockPreferencesRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserPreferences?)null);

        // Mock compatibility calculations
        _mockCompatibilityAgent.ExecuteAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var request = callInfo.Arg<AgentRequest>();
                var userId2 = request.Parameters["userId2"].ToString();
                var score = userId2 == candidate1.Id.ToString() ? 85 : 75;
                
                return Task.FromResult(AgentResult.CreateSuccess(new Dictionary<string, object>
                {
                    ["compatibilityScore"] = score,
                    ["detailedScore"] = new Dictionary<string, object>
                    {
                        ["overallScore"] = score
                    }
                }));
            });

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString(),
                ["maxResults"] = 10
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("rankedMatches");
        
        var matches = data!["rankedMatches"] as List<Dictionary<string, object>>;
        matches.Should().NotBeNull();
        matches!.Count.Should().Be(2);
        
        // First match should have higher score
        var firstMatch = matches[0];
        var secondMatch = matches[1];
        Convert.ToInt32(firstMatch["compatibilityScore"]).Should().BeGreaterThan(
            Convert.ToInt32(secondMatch["compatibilityScore"]));
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
    public async Task ExecuteAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

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
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithUserPreferences_ShouldApplyPreferenceWeighting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUser = CreateTestUser(userId, "John", "hiking, cooking");
        var candidate = CreateTestUser(Guid.NewGuid(), "Jane", "hiking, photography");
        
        var preferences = new UserPreferences { UserId = userId };
        preferences.UpdateInterestWeight("hiking", 0.9);
        preferences.UpdateInterestWeight("photography", 0.1);
        
        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(currentUser);
        _mockUserRepository.GetPotentialMatchesAsync(userId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { candidate });

        _mockPreferencesRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(preferences);

        _mockCompatibilityAgent.ExecuteAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>())
            .Returns(AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["compatibilityScore"] = 75,
                ["detailedScore"] = new Dictionary<string, object>
                {
                    ["overallScore"] = 75
                }
            }));

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
        data.Should().ContainKey("rankedMatches");
        data.Should().ContainKey("preferencesApplied");
        data!["preferencesApplied"].Should().Be(true);
    }

    [Fact]
    public async Task ExecuteAsync_WithMaxResults_ShouldLimitResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentUser = CreateTestUser(userId, "John", "hiking");
        var candidates = Enumerable.Range(1, 5)
            .Select(i => CreateTestUser(Guid.NewGuid(), $"User{i}", "hiking"))
            .ToList();
        
        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(currentUser);
        _mockUserRepository.GetPotentialMatchesAsync(userId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(candidates);

        _mockPreferencesRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserPreferences?)null);

        _mockCompatibilityAgent.ExecuteAsync(Arg.Any<AgentRequest>(), Arg.Any<CancellationToken>())
            .Returns(AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["compatibilityScore"] = 70,
                ["detailedScore"] = new Dictionary<string, object>
                {
                    ["overallScore"] = 70
                }
            }));

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString(),
                ["maxResults"] = 3
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        var matches = data!["rankedMatches"] as List<Dictionary<string, object>>;
        matches!.Count.Should().Be(3);
    }

    private static User CreateTestUser(Guid id, string firstName, string interests)
    {
        return new User
        {
            Email = $"{firstName.ToLower()}@example.com",
            FirstName = firstName,
            LastName = "Doe",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
            Bio = $"I'm {firstName}",
            Profile = new UserProfile
            {
                UserId = id,
                Interests = interests,
                Location = "San Francisco, CA",
                Occupation = "Software Engineer",
                GenderIdentity = GenderIdentity.Female,
                InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Male },
                MinAge = 22,
                MaxAge = 35
            }
        };
    }
}
