using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Dating;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;
using Xunit;

namespace SoulSync.Agents.Tests;

public class DateSuggestionAgentTests
{
    private readonly IDateSuggestionRepository _mockDateSuggestionRepository;
    private readonly IMatchRepository _mockMatchRepository;
    private readonly IUserRepository _mockUserRepository;
    private readonly IAIService _mockAIService;
    private readonly ILogger<DateSuggestionAgent> _mockLogger;
    private readonly DateSuggestionAgent _agent;
    
    public DateSuggestionAgentTests()
    {
        _mockDateSuggestionRepository = Substitute.For<IDateSuggestionRepository>();
        _mockMatchRepository = Substitute.For<IMatchRepository>();
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockAIService = Substitute.For<IAIService>();
        _mockLogger = Substitute.For<ILogger<DateSuggestionAgent>>();
        
        _agent = new DateSuggestionAgent(
            _mockDateSuggestionRepository,
            _mockMatchRepository,
            _mockUserRepository,
            _mockAIService,
            _mockLogger);
    }
    
    [Fact]
    public async Task ExecuteAsync_GenerateSuggestions_WithValidMatch_ShouldCreateSuggestions()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var user1 = CreateUser("John", "hiking, coffee, movies");
        var user2 = CreateUser("Jane", "coffee, art, hiking");
        var match = new Match { Id = matchId, UserId1 = user1.Id, UserId2 = user2.Id, CompatibilityScore = 85 };
        
        _mockMatchRepository.GetByIdAsync(matchId, Arg.Any<CancellationToken>())
            .Returns(match);
        
        _mockUserRepository.GetByIdAsync(user1.Id, Arg.Any<CancellationToken>())
            .Returns(user1);
        
        _mockUserRepository.GetByIdAsync(user2.Id, Arg.Any<CancellationToken>())
            .Returns(user2);
        
        _mockAIService.ProcessRequestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("1. Coffee Date at Blue Bottle - Perfect spot for conversation\n2. Hiking at Griffith Park - Beautiful trails and views\n3. Art Gallery Tour - LACMA weekend exhibition");
        
        _mockDateSuggestionRepository.CreateAsync(Arg.Any<DateSuggestion>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<DateSuggestion>());
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "generate",
                ["matchId"] = matchId.ToString(),
                ["count"] = 3
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        var suggestions = data!["suggestions"] as List<Dictionary<string, object>>;
        suggestions.Should().NotBeNull();
        suggestions!.Count.Should().BeGreaterThanOrEqualTo(1);
        
        await _mockDateSuggestionRepository.Received().CreateAsync(
            Arg.Any<DateSuggestion>(),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ExecuteAsync_GenerateSuggestions_WithMissingMatchId_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "generate"
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("matchId");
    }
    
    [Fact]
    public async Task ExecuteAsync_GenerateSuggestions_WithNonExistentMatch_ShouldReturnError()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        
        _mockMatchRepository.GetByIdAsync(matchId, Arg.Any<CancellationToken>())
            .Returns((Match?)null);
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "generate",
                ["matchId"] = matchId.ToString()
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Match not found");
    }
    
    [Fact]
    public async Task ExecuteAsync_AcceptSuggestion_WithValidSuggestion_ShouldAcceptSuggestion()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Coffee Date",
            Description = "Cozy coffee shop",
            Location = "Downtown"
        };
        
        _mockDateSuggestionRepository.GetByIdAsync(suggestionId, Arg.Any<CancellationToken>())
            .Returns(suggestion);
        
        _mockDateSuggestionRepository.UpdateAsync(Arg.Any<DateSuggestion>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<DateSuggestion>());
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "accept",
                ["suggestionId"] = suggestionId.ToString()
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        data!["status"].Should().Be("Accepted");
        
        await _mockDateSuggestionRepository.Received(1).UpdateAsync(
            Arg.Is<DateSuggestion>(s => s.IsAccepted),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ExecuteAsync_RejectSuggestion_WithValidSuggestion_ShouldRejectSuggestion()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Hiking",
            Description = "Mountain trail",
            Location = "State Park"
        };
        
        _mockDateSuggestionRepository.GetByIdAsync(suggestionId, Arg.Any<CancellationToken>())
            .Returns(suggestion);
        
        _mockDateSuggestionRepository.UpdateAsync(Arg.Any<DateSuggestion>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<DateSuggestion>());
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "reject",
                ["suggestionId"] = suggestionId.ToString(),
                ["reason"] = "Not interested in outdoor activities"
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        data!["status"].Should().Be("Rejected");
        
        await _mockDateSuggestionRepository.Received(1).UpdateAsync(
            Arg.Is<DateSuggestion>(s => s.IsRejected),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ExecuteAsync_GetSuggestions_WithValidMatchId_ShouldReturnSuggestions()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var suggestions = new List<DateSuggestion>
        {
            new DateSuggestion { MatchId = matchId, Title = "Coffee", Location = "Cafe A" },
            new DateSuggestion { MatchId = matchId, Title = "Dinner", Location = "Restaurant B" }
        };
        
        _mockDateSuggestionRepository.GetByMatchIdAsync(matchId, Arg.Any<CancellationToken>())
            .Returns(suggestions);
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "get",
                ["matchId"] = matchId.ToString()
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        var returnedSuggestions = data!["suggestions"] as List<Dictionary<string, object>>;
        returnedSuggestions.Should().NotBeNull();
        returnedSuggestions!.Count.Should().Be(2);
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenAIServiceFails_ShouldUseFallbackSuggestions()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var user1 = CreateUser("Alice", "music, dining");
        var user2 = CreateUser("Bob", "music, sports");
        var match = new Match { Id = matchId, UserId1 = user1.Id, UserId2 = user2.Id };
        
        _mockMatchRepository.GetByIdAsync(matchId, Arg.Any<CancellationToken>())
            .Returns(match);
        
        _mockUserRepository.GetByIdAsync(user1.Id, Arg.Any<CancellationToken>())
            .Returns(user1);
        
        _mockUserRepository.GetByIdAsync(user2.Id, Arg.Any<CancellationToken>())
            .Returns(user2);
        
        _mockAIService.ProcessRequestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new Exception("AI service unavailable")));
        
        _mockDateSuggestionRepository.CreateAsync(Arg.Any<DateSuggestion>(), Arg.Any<CancellationToken>())
            .Returns(args => args.Arg<DateSuggestion>());
        
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "generate",
                ["matchId"] = matchId.ToString()
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        var data = result.Data as Dictionary<string, object>;
        data.Should().NotBeNull();
        var suggestions = data!["suggestions"] as List<Dictionary<string, object>>;
        suggestions.Should().NotBeNull();
        suggestions!.Count.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithInvalidAction_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Parameters = new Dictionary<string, object>
            {
                ["action"] = "invalid_action"
            }
        };
        
        // Act
        var result = await _agent.ExecuteAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid action");
    }
    
    private static User CreateUser(string name, string interests)
    {
        return new User
        {
            FirstName = name,
            LastName = "Test",
            Email = $"{name.ToLower()}@test.com",
            Profile = new UserProfile
            {
                Interests = interests
            }
        };
    }
}
