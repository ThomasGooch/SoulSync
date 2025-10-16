using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;
using SoulSync.Web.Components.Pages;

namespace SoulSync.Web.Tests.Components.Pages;

public class ProfileTests : TestContext
{
    private readonly IUserRepository _userRepository;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ProfileTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        Services.AddSingleton(_userRepository);
    }

    [Fact]
    public void Profile_ShouldRender_WithUserDetails()
    {
        // Arrange
        var testUser = CreateTestUser();
        _userRepository.GetByIdAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns(testUser);

        // Act
        var cut = RenderComponent<Profile>(parameters => parameters
            .Add(p => p.UserId, _testUserId.ToString()));

        // Wait for async initialization
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain(testUser.FirstName);
            cut.Markup.Should().Contain(testUser.LastName);
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Profile_ShouldDisplay_ProfilePhoto()
    {
        // Arrange
        var testUser = CreateTestUser();
        _userRepository.GetByIdAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns(testUser);

        // Act
        var cut = RenderComponent<Profile>(parameters => parameters
            .Add(p => p.UserId, _testUserId.ToString()));

        // Assert
        cut.Markup.Should().Contain("img");
    }

    [Fact]
    public void Profile_ShouldDisplay_Bio()
    {
        // Arrange
        var testUser = CreateTestUser();
        _userRepository.GetByIdAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns(testUser);

        // Act
        var cut = RenderComponent<Profile>(parameters => parameters
            .Add(p => p.UserId, _testUserId.ToString()));

        // Assert
        cut.Markup.Should().Contain(testUser.Bio);
    }

    [Fact]
    public void Profile_ShouldDisplay_InterestsWhenProfileExists()
    {
        // Arrange
        var testUser = CreateTestUser();
        _userRepository.GetByIdAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns(testUser);

        // Act
        var cut = RenderComponent<Profile>(parameters => parameters
            .Add(p => p.UserId, _testUserId.ToString()));

        // Assert
        cut.Markup.Should().Contain("hiking");
        cut.Markup.Should().Contain("photography");
    }

    [Fact]
    public async Task Profile_WhenUserNotFound_ShouldDisplayErrorMessage()
    {
        // Arrange
        _userRepository.GetByIdAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var cut = RenderComponent<Profile>(parameters => parameters
            .Add(p => p.UserId, _testUserId.ToString()));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("not found") || cut.Markup.Contains("User not found"), 
            timeout: TimeSpan.FromSeconds(2));
    }

    private User CreateTestUser()
    {
        return new User
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25)),
            Bio = "I love hiking and photography",
            Profile = new UserProfile
            {
                UserId = _testUserId,
                Interests = "hiking,photography,travel",
                Location = "San Francisco, CA",
                GenderIdentity = SoulSync.Core.Enums.GenderIdentity.Male
            }
        };
    }
}
