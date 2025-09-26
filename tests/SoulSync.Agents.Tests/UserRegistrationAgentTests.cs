using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Agents.Registration;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Tests;

public class UserRegistrationAgentTests
{
    private readonly IUserRepository _mockUserRepository;
    private readonly IAIService _mockAIService;
    private readonly ILogger<UserRegistrationAgent> _mockLogger;
    private readonly UserRegistrationAgent _agent;

    public UserRegistrationAgentTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockAIService = Substitute.For<IAIService>();
        _mockLogger = Substitute.For<ILogger<UserRegistrationAgent>>();
        
        _agent = new UserRegistrationAgent(_mockAIService, _mockUserRepository, _mockLogger);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidUserData_ShouldCreateUserAndProfile()
    {
        // Arrange
        var request = new AgentRequest
        {
            Message = "Register new user",
            Parameters = new Dictionary<string, object>
            {
                ["email"] = "john@example.com",
                ["firstName"] = "John",
                ["lastName"] = "Doe",
                ["dateOfBirth"] = "1995-05-15",
                ["bio"] = "Love hiking and cooking, passionate about outdoor adventures",
                ["genderIdentity"] = "Male",
                ["interestedInGenders"] = new List<string> { "Female" },
                ["interests"] = "hiking, cooking, travel, photography",
                ["location"] = "San Francisco, CA",
                ["occupation"] = "Software Engineer"
            }
        };

        _mockUserRepository.ExistsAsync("john@example.com", Arg.Any<CancellationToken>())
            .Returns(false);

        _mockAIService.AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Profile shows outdoor enthusiasm and culinary passion. Suggests extroversion and creativity.");

        _mockUserRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                // Return the same user that was passed in, so it includes the profile and AI insights
                var userArg = callInfo.Arg<User>();
                return userArg;
            });

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var responseData = result.Data as Dictionary<string, object>;
        responseData.Should().ContainKey("userId");
        responseData.Should().ContainKey("aiInsights");
        responseData!["aiInsights"].Should().Be("Profile shows outdoor enthusiasm and culinary passion. Suggests extroversion and creativity.");

        await _mockUserRepository.Received(1).CreateAsync(
            Arg.Is<User>(u => u.Email == "john@example.com" && u.FirstName == "John"),
            Arg.Any<CancellationToken>());

        await _mockAIService.Received(1).AnalyzeProfileAsync(
            Arg.Is<string>(s => s.Contains("hiking") && s.Contains("cooking")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingEmail_ShouldReturnError()
    {
        // Arrange
        var request = new AgentRequest
        {
            Message = "Register new user",
            Parameters = new Dictionary<string, object>
            {
                ["email"] = "existing@example.com",
                ["firstName"] = "John",
                ["lastName"] = "Doe",
                ["dateOfBirth"] = "1995-05-15"
            }
        };

        _mockUserRepository.ExistsAsync("existing@example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");

        await _mockUserRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _mockAIService.DidNotReceive().AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "firstName")]
    [InlineData("invalid-email", "email")]
    [InlineData("2010-05-15", "dateOfBirth")] // Too young
    public async Task ExecuteAsync_WithInvalidData_ShouldReturnValidationError(string invalidValue, string parameterName)
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["email"] = "john@example.com",
            ["firstName"] = "John",
            ["lastName"] = "Doe",
            ["dateOfBirth"] = "1995-05-15"
        };

        // Override with invalid value
        if (parameterName == "dateOfBirth")
            parameters[parameterName] = invalidValue;
        else
            parameters[parameterName] = invalidValue;

        var request = new AgentRequest
        {
            Message = "Register new user",
            Parameters = parameters
        };

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();

        await _mockUserRepository.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAIServiceFails_ShouldStillCreateUserWithoutInsights()
    {
        // Arrange
        var request = new AgentRequest
        {
            Message = "Register new user",
            Parameters = new Dictionary<string, object>
            {
                ["email"] = "john@example.com",
                ["firstName"] = "John",
                ["lastName"] = "Doe",
                ["dateOfBirth"] = "1995-05-15",
                ["bio"] = "Love hiking and cooking"
            }
        };

        _mockUserRepository.ExistsAsync("john@example.com", Arg.Any<CancellationToken>())
            .Returns(false);

        _mockAIService.AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new Exception("AI service unavailable")));

        var createdUser = new User
        {
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1995, 5, 15)
        };

        _mockUserRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var responseData = result.Data as Dictionary<string, object>;
        responseData.Should().ContainKey("userId");
        responseData.Should().ContainKey("aiInsights");
        responseData!["aiInsights"].Should().Be("Profile analysis will be completed shortly.");

        await _mockUserRepository.Received(1).CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithMinimalRequiredFields_ShouldCreateUser()
    {
        // Arrange
        var request = new AgentRequest
        {
            Message = "Register new user",
            Parameters = new Dictionary<string, object>
            {
                ["email"] = "minimal@example.com",
                ["firstName"] = "Jane",
                ["lastName"] = "Smith",
                ["dateOfBirth"] = "1990-03-20"
            }
        };

        _mockUserRepository.ExistsAsync("minimal@example.com", Arg.Any<CancellationToken>())
            .Returns(false);

        var createdUser = new User
        {
            Email = "minimal@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            DateOfBirth = new DateOnly(1990, 3, 20)
        };

        _mockUserRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        await _mockUserRepository.Received(1).CreateAsync(
            Arg.Is<User>(u => u.Email == "minimal@example.com" && u.Profile == null),
            Arg.Any<CancellationToken>());

        // Should not call AI service if no bio/interests provided
        await _mockAIService.DidNotReceive().AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithCompleteProfileData_ShouldCreateUserWithProfile()
    {
        // Arrange
        var request = new AgentRequest
        {
            Message = "Register new user",
            Parameters = new Dictionary<string, object>
            {
                ["email"] = "complete@example.com",
                ["firstName"] = "Alex",
                ["lastName"] = "Johnson",
                ["dateOfBirth"] = "1992-08-10",
                ["bio"] = "Passionate about technology and outdoor activities",
                ["genderIdentity"] = "NonBinary",
                ["interestedInGenders"] = new List<string> { "Male", "Female", "NonBinary" },
                ["interests"] = "programming, hiking, photography, music",
                ["location"] = "Seattle, WA",
                ["occupation"] = "DevOps Engineer",
                ["minAge"] = 25,
                ["maxAge"] = 35,
                ["maxDistanceKm"] = 50
            }
        };

        _mockUserRepository.ExistsAsync("complete@example.com", Arg.Any<CancellationToken>())
            .Returns(false);

        _mockAIService.AnalyzeProfileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Technologically minded with outdoor interests. Shows analytical thinking and adventure-seeking personality.");

        var createdUser = new User
        {
            Email = "complete@example.com",
            FirstName = "Alex",
            LastName = "Johnson",
            DateOfBirth = new DateOnly(1992, 8, 10)
        };

        _mockUserRepository.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(createdUser);

        // Act
        var result = await _agent.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        await _mockUserRepository.Received(1).CreateAsync(
            Arg.Is<User>(u => 
                u.Email == "complete@example.com" && 
                u.Profile != null &&
                u.Profile.GenderIdentity == GenderIdentity.NonBinary &&
                u.Profile.Interests == "programming, hiking, photography, music" &&
                u.Profile.MinAge == 25 &&
                u.Profile.MaxAge == 35),
            Arg.Any<CancellationToken>());
    }
}