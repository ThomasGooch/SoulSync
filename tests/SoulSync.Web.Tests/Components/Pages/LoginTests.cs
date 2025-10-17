using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SoulSync.Core.Authentication;
using SoulSync.Core.Interfaces;
using SoulSync.Web.Components.Pages;

namespace SoulSync.Web.Tests.Components.Pages;

public class LoginTests : TestContext
{
    private readonly IAuthenticationService _authService;

    public LoginTests()
    {
        _authService = Substitute.For<IAuthenticationService>();
        Services.AddSingleton(_authService);
        
        // Mock NavigationManager
        var navManager = Substitute.For<NavigationManager>();
        Services.AddSingleton(navManager);
    }

    [Fact]
    public void Login_ShouldRender_WithEmailAndPasswordFields()
    {
        // Arrange & Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Find("input[type='email']").Should().NotBeNull();
        cut.Find("input[type='password']").Should().NotBeNull();
        cut.Find("button[type='submit']").Should().NotBeNull();
    }

    [Fact]
    public void Login_ShouldDisplay_PageTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Markup.Should().Contain("Login");
    }

    [Fact]
    public void Login_ShouldDisplay_LinkToRegistration()
    {
        // Arrange & Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Markup.Should().Contain("/register");
    }

    [Fact]
    public async Task Login_WhenSubmitted_ShouldCallAuthenticationService()
    {
        // Arrange
        var cut = RenderComponent<Login>();
        var emailInput = cut.Find("input[type='email']");
        var passwordInput = cut.Find("input[type='password']");
        var form = cut.Find("form");

        _authService.AuthenticateAsync(Arg.Any<AuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AuthenticationResult
            {
                IsSuccess = true,
                Token = "test-token",
                User = new SoulSync.Core.Domain.User
                {
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-25))
                }
            });

        // Act
        emailInput.Change("test@example.com");
        passwordInput.Change("password123");
        await form.SubmitAsync();

        // Assert
        await _authService.Received(1).AuthenticateAsync(
            Arg.Is<AuthenticationRequest>(r => r.Email == "test@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_WhenInvalidCredentials_ShouldDisplayErrorMessage()
    {
        // Arrange
        var cut = RenderComponent<Login>();
        var emailInput = cut.Find("input[type='email']");
        var passwordInput = cut.Find("input[type='password']");
        var form = cut.Find("form");

        _authService.AuthenticateAsync(Arg.Any<AuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "Invalid credentials"
            });

        // Act
        emailInput.Change("test@example.com");
        passwordInput.Change("wrongpassword");
        await form.SubmitAsync();

        // Assert
        cut.Markup.Should().Contain("Invalid credentials");
    }

    [Fact]
    public void Login_WhenEmailEmpty_ShouldShowValidationError()
    {
        // Arrange
        var cut = RenderComponent<Login>();
        var passwordInput = cut.Find("input[type='password']");
        var form = cut.Find("form");

        // Act
        passwordInput.Change("password123");
        form.Submit();

        // Assert
        cut.Markup.Should().Contain("required");
    }
}
