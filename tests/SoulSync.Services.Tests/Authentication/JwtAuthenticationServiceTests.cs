using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SoulSync.Core.Authentication;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;
using SoulSync.Services.Authentication;

namespace SoulSync.Services.Tests.Authentication;

public class JwtAuthenticationServiceTests
{
    private readonly IUserRepository _mockUserRepository;
    private readonly IConfiguration _mockConfiguration;
    private readonly ILogger<JwtAuthenticationService> _mockLogger;
    private readonly JwtAuthenticationService _authService;

    public JwtAuthenticationServiceTests()
    {
        _mockUserRepository = Substitute.For<IUserRepository>();
        _mockConfiguration = Substitute.For<IConfiguration>();
        _mockLogger = Substitute.For<ILogger<JwtAuthenticationService>>();

        // Setup configuration
        _mockConfiguration["JWT:SigningKey"].Returns("super-secret-key-that-is-at-least-32-characters-long-for-security");
        _mockConfiguration["JWT:Issuer"].Returns("SoulSync");
        _mockConfiguration["JWT:Audience"].Returns("SoulSyncUsers");
        _mockConfiguration["JWT:ExpirationMinutes"].Returns("60");

        _authService = new JwtAuthenticationService(_mockUserRepository, _mockConfiguration, _mockLogger);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var email = "john@example.com";
        var password = "SecurePassword123!";
        
        var user = new User
        {
            Email = email,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1995, 5, 15)
        };
        // User is active by default

        _mockUserRepository.GetByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns(user);

        // Mock password verification (we'll implement this later)
        var request = new AuthenticationRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await _authService.AuthenticateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(email);
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var request = new AuthenticationRequest
        {
            Email = "nonexistent@example.com",
            Password = "password"
        };

        _mockUserRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _authService.AuthenticateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid credentials");
        result.Token.Should().BeNullOrEmpty();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ShouldReturnFailure()
    {
        // Arrange
        var email = "john@example.com";
        var user = new User
        {
            Email = email,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1995, 5, 15)
        };

        _mockUserRepository.GetByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new AuthenticationRequest
        {
            Email = email,
            Password = "WrongPassword"
        };

        // Act
        var result = await _authService.AuthenticateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid credentials");
        result.Token.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_WithInactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var email = "inactive@example.com";
        var user = new User
        {
            Email = email,
            FirstName = "Inactive",
            LastName = "User",
            DateOfBirth = new DateOnly(1990, 1, 1)
        };
        user.Deactivate(); // Make user inactive

        _mockUserRepository.GetByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new AuthenticationRequest
        {
            Email = email,
            Password = "ValidPassword123!"
        };

        // Act
        var result = await _authService.AuthenticateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("account is deactivated");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task AuthenticateAsync_WithEmptyEmail_ShouldReturnValidationError(string email)
    {
        // Arrange
        var request = new AuthenticationRequest
        {
            Email = email,
            Password = "ValidPassword123!"
        };

        // Act
        var result = await _authService.AuthenticateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Email is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task AuthenticateAsync_WithEmptyPassword_ShouldReturnValidationError(string password)
    {
        // Arrange
        var request = new AuthenticationRequest
        {
            Email = "john@example.com",
            Password = password
        };

        // Act
        var result = await _authService.AuthenticateAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Password is required");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewToken()
    {
        // Arrange - First authenticate to get a valid refresh token
        var user = new User
        {
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1995, 5, 15)
        };

        _mockUserRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        _mockUserRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var authRequest = new AuthenticationRequest
        {
            Email = user.Email,
            Password = "ValidPassword123!"
        };

        var authResult = await _authService.AuthenticateAsync(authRequest);
        var refreshToken = authResult.RefreshToken!;

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(refreshToken); // Should be a new refresh token
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var invalidRefreshToken = "invalid-token";

        // Act
        var result = await _authService.RefreshTokenAsync(invalidRefreshToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid refresh token");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnUserClaims()
    {
        // Arrange
        // First authenticate to get a valid token
        var user = new User
        {
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1995, 5, 15)
        };

        _mockUserRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        var authRequest = new AuthenticationRequest
        {
            Email = user.Email,
            Password = "ValidPassword123!"
        };

        var authResult = await _authService.AuthenticateAsync(authRequest);
        var token = authResult.Token!;

        // Act
        var validationResult = await _authService.ValidateTokenAsync(token);

        // Assert
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue();
        validationResult.UserId.Should().Be(user.Id);
        validationResult.Email.Should().Be(user.Email);
        validationResult.Claims.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-token")]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid")]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnInvalidResult(string token)
    {
        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidRefreshToken_ShouldSucceed()
    {
        // Arrange - First authenticate to get a valid refresh token
        var user = new User
        {
            Email = "revoke@example.com",
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = new DateOnly(1990, 1, 1)
        };

        _mockUserRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        var authRequest = new AuthenticationRequest
        {
            Email = user.Email,
            Password = "ValidPassword123!"
        };

        var authResult = await _authService.AuthenticateAsync(authRequest);
        var refreshToken = authResult.RefreshToken!;

        // Act
        var result = await _authService.RevokeTokenAsync(refreshToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HashPasswordAsync_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hashedPassword = await _authService.HashPasswordAsync(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password); // Should be hashed
        hashedPassword.Length.Should().BeGreaterThan(password.Length);
    }

    [Fact]
    public async Task VerifyPasswordAsync_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hashedPassword = await _authService.HashPasswordAsync(password);

        // Act
        var isValid = await _authService.VerifyPasswordAsync(password, hashedPassword);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyPasswordAsync_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = await _authService.HashPasswordAsync(correctPassword);

        // Act
        var isValid = await _authService.VerifyPasswordAsync(wrongPassword, hashedPassword);

        // Assert
        isValid.Should().BeFalse();
    }
}