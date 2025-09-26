using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Analysis;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Tests;

public class ProfileAnalysisAgentTests
{
    private readonly IUserRepository _mockUserRepository;
    private readonly IAIService _mockAIService;
    private readonly ILogger<ProfileAnalysisAgent> _mockLogger;
    private readonly ProfileAnalysisAgent _agent;

    public ProfileAnalysisAgentTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockAIService = Substitute.For<IAIService>();
        _mockLogger = Substitute.For<ILogger<ProfileAnalysisAgent>>();
        
        _agent = new ProfileAnalysisAgent(_mockAIService, _mockUserRepository, _mockLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidUserId_ShouldAnalyzeAndUpdateProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AgentRequest
        {
            Message = "Analyze user profile",
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        var user = new User
        {
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1995, 5, 15),
            Bio = "Passionate about outdoor adventures and cooking"
        };

        user.Profile = new UserProfile
        {
            UserId = userId,
            Interests = "hiking, cooking, photography, travel",
            Location = "San Francisco, CA",
            Occupation = "Software Engineer",
            GenderIdentity = GenderIdentity.Male,
            InterestedInGenders = new List<GenderIdentity> { GenderIdentity.Female }
        };

        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _mockAIService.AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Analysis: Outdoor enthusiast with culinary interests. Shows adventurous spirit, creativity, and technical mindset. Likely extroverted with strong problem-solving skills.");

        _mockUserRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var responseData = result.Data as Dictionary<string, object>;
        responseData.Should().ContainKey("userId");
        responseData.Should().ContainKey("insights");
        responseData.Should().ContainKey("personalityTraits");
        responseData.Should().ContainKey("interestCategories");
        
        responseData!["insights"].Should().Be("Analysis: Outdoor enthusiast with culinary interests. Shows adventurous spirit, creativity, and technical mindset. Likely extroverted with strong problem-solving skills.");

        await _mockUserRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.Profile!.HasAIAnalysis && u.Profile.AIInsights!.Contains("Outdoor enthusiast")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AgentRequest
        {
            Message = "Analyze user profile",
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");

        await _mockUserRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _mockAIService.DidNotReceive().AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidUserId_ShouldReturnValidationError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Message = "Analyze user profile",
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = "invalid-guid"
            }
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid user ID");

        await _mockUserRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingUserId_ShouldReturnValidationError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Message = "Analyze user profile",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User ID is required");
    }

    [Fact]
    public async Task ExecuteAsync_WithUserWithoutProfile_ShouldAnalyzeBasicInfo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AgentRequest
        {
            Message = "Analyze user profile",
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        var user = new User
        {
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            DateOfBirth = new DateOnly(1992, 3, 20),
            Bio = "Love reading and exploring new places"
        };
        // No Profile set

        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _mockAIService.AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Basic analysis: Shows curiosity and love for learning. Enjoys intellectual pursuits and travel.");

        _mockUserRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var responseData = result.Data as Dictionary<string, object>;
        responseData!["insights"].Should().Be("Basic analysis: Shows curiosity and love for learning. Enjoys intellectual pursuits and travel.");

        await _mockAIService.Received(1).AnalyzeProfileAsync(
            Arg.Is<string>(s => s.Contains("reading") && s.Contains("exploring")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAIServiceFails_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AgentRequest
        {
            Message = "Analyze user profile",
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Bio = "Test bio"
        };

        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _mockAIService.AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new Exception("AI service unavailable")));

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to analyze profile");

        await _mockUserRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExtractPersonalityTraits()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AgentRequest
        {
            Message = "Analyze user profile",
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        var user = new User
        {
            Email = "creative@example.com",
            FirstName = "Alex",
            LastName = "Artist",
            DateOfBirth = new DateOnly(1988, 7, 15),
            Bio = "Creative soul who loves painting, music, and meeting new people"
        };

        user.Profile = new UserProfile
        {
            UserId = userId,
            Interests = "painting, music, art galleries, concerts",
            Occupation = "Graphic Designer"
        };

        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _mockAIService.AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Creative personality with strong artistic interests. Extroverted and socially engaged. Values self-expression and aesthetic experiences.");

        _mockUserRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var responseData = result.Data as Dictionary<string, object>;
        responseData.Should().ContainKey("personalityTraits");
        
        var traits = responseData!["personalityTraits"] as List<string>;
        traits.Should().Contain("Creative");
        traits.Should().Contain("Artistic");
        traits.Should().Contain("Social");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCategorizeInterests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AgentRequest
        {
            Message = "Analyze user profile",
            Parameters = new Dictionary<string, object>
            {
                ["userId"] = userId.ToString()
            }
        };

        var user = new User
        {
            Email = "sports@example.com",
            FirstName = "Sam",
            LastName = "Athletic",
            DateOfBirth = new DateOnly(1985, 11, 8),
            Bio = "Fitness enthusiast and tech lover"
        };

        user.Profile = new UserProfile
        {
            UserId = userId,
            Interests = "running, cycling, programming, gadgets, hiking",
            Occupation = "DevOps Engineer"
        };

        _mockUserRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        _mockAIService.AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Health-conscious individual with strong technical background. Combines physical fitness with technology interests.");

        _mockUserRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<User>());

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var responseData = result.Data as Dictionary<string, object>;
        responseData.Should().ContainKey("interestCategories");
        
        var categories = responseData!["interestCategories"] as Dictionary<string, List<string>>;
        categories.Should().ContainKey("Sports & Fitness");
        categories.Should().ContainKey("Technology");
        categories!["Sports & Fitness"].Should().Contain("running");
        categories["Technology"].Should().Contain("programming");
    }
}