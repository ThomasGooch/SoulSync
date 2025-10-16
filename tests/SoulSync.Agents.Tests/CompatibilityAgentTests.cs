using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Matching;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Tests;

public class CompatibilityAgentTests
{
    private readonly IUserRepository _mockUserRepository;
    private readonly IAIService _mockAIService;
    private readonly ILogger<CompatibilityAgent> _mockLogger;
    private readonly CompatibilityAgent _agent;

    public CompatibilityAgentTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockAIService = Substitute.For<IAIService>();
        _mockLogger = Substitute.For<ILogger<CompatibilityAgent>>();
        
        _agent = new CompatibilityAgent(_mockUserRepository, _mockAIService, _mockLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidUserIds_ShouldCalculateCompatibility()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        
        var user1 = CreateTestUser(user1Id, "John", "hiking, cooking, travel");
        var user2 = CreateTestUser(user2Id, "Jane", "hiking, photography, travel");

        _mockUserRepository.GetByIdAsync(user1Id, Arg.Any<CancellationToken>()).Returns(user1);
        _mockUserRepository.GetByIdAsync(user2Id, Arg.Any<CancellationToken>()).Returns(user2);
        
        _mockAIService.CalculateCompatibilityScoreAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(85);

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId1"] = user1Id.ToString(),
                ["userId2"] = user2Id.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("compatibilityScore");
        data.Should().ContainKey("detailedScore");
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingUserId_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId1"] = Guid.NewGuid().ToString()
                // Missing userId2
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("userId2");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        
        _mockUserRepository.GetByIdAsync(user1Id, Arg.Any<CancellationToken>()).Returns(CreateTestUser(user1Id, "John", "hiking"));
        _mockUserRepository.GetByIdAsync(user2Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId1"] = user1Id.ToString(),
                ["userId2"] = user2Id.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithCommonInterests_ShouldReturnHigherScore()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        
        var user1 = CreateTestUser(user1Id, "John", "hiking, cooking, travel, photography");
        var user2 = CreateTestUser(user2Id, "Jane", "hiking, cooking, travel, photography");

        _mockUserRepository.GetByIdAsync(user1Id, Arg.Any<CancellationToken>()).Returns(user1);
        _mockUserRepository.GetByIdAsync(user2Id, Arg.Any<CancellationToken>()).Returns(user2);
        
        _mockAIService.CalculateCompatibilityScoreAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(95);

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId1"] = user1Id.ToString(),
                ["userId2"] = user2Id.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        var score = Convert.ToInt32(data!["compatibilityScore"]);
        score.Should().BeGreaterThan(70);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAIServiceFails_ShouldUseFallbackCalculation()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        
        var user1 = CreateTestUser(user1Id, "John", "hiking, cooking");
        var user2 = CreateTestUser(user2Id, "Jane", "hiking, photography");

        _mockUserRepository.GetByIdAsync(user1Id, Arg.Any<CancellationToken>()).Returns(user1);
        _mockUserRepository.GetByIdAsync(user2Id, Arg.Any<CancellationToken>()).Returns(user2);
        
        _mockAIService.CalculateCompatibilityScoreAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<int>(callInfo => throw new Exception("AI service unavailable"));

        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["userId1"] = user1Id.ToString(),
                ["userId2"] = user2Id.ToString()
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().ContainKey("compatibilityScore");
        data!["compatibilityScore"].Should().NotBeNull();
    }

    private static User CreateTestUser(Guid id, string firstName, string interests)
    {
        return new User
        {
            Email = $"{firstName.ToLower()}@example.com",
            FirstName = firstName,
            LastName = "Doe",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
            Bio = $"I'm {firstName} and I love exploring new things!",
            Profile = new UserProfile
            {
                UserId = id,
                Interests = interests,
                Location = "San Francisco, CA",
                Occupation = "Software Engineer",
                GenderIdentity = GenderIdentity.Male,
                InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Female },
                MinAge = 22,
                MaxAge = 35
            }
        };
    }
}
