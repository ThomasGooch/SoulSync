using FluentAssertions;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;

namespace SoulSync.Core.Tests.Domain;

public class UserTests
{
    [Fact]
    public void User_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // Arrange
        var email = "john@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var dateOfBirth = new DateOnly(1995, 5, 15);

        // Act
        var user = new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth
        };

        // Assert
        user.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.DateOfBirth.Should().Be(dateOfBirth);
        user.Age.Should().Be(DateTime.Now.Year - 1995); // Approximate age calculation
        user.FullName.Should().Be("John Doe");
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData("", "First name cannot be empty")]
    [InlineData(null, "First name cannot be empty")]
    [InlineData("   ", "First name cannot be empty")]
    public void User_WhenCreatedWithInvalidFirstName_ShouldThrowArgumentException(string firstName, string expectedMessage)
    {
        // Act & Assert
        var act = () => new User
        {
            FirstName = firstName,
            LastName = "Doe",
            Email = "john@example.com",
            DateOfBirth = new DateOnly(1995, 5, 15)
        };

        act.Should().Throw<ArgumentException>()
           .WithMessage($"*{expectedMessage}*");
    }

    [Theory]
    [InlineData("", "Email cannot be empty")]
    [InlineData(null, "Email cannot be empty")]
    [InlineData("invalid-email", "Email format is invalid")]
    [InlineData("@domain.com", "Email format is invalid")]
    [InlineData("user@", "Email format is invalid")]
    public void User_WhenCreatedWithInvalidEmail_ShouldThrowArgumentException(string email, string expectedMessage)
    {
        // Act & Assert
        var act = () => new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            DateOfBirth = new DateOnly(1995, 5, 15)
        };

        act.Should().Throw<ArgumentException>()
           .WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public void User_WhenCreatedWithFutureDateOfBirth_ShouldThrowArgumentException()
    {
        // Arrange
        var futureDateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(1));

        // Act & Assert
        var act = () => new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            DateOfBirth = futureDateOfBirth
        };

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Date of birth cannot be in the future*");
    }

    [Fact]
    public void User_WhenCreatedWithAgeLessThan18_ShouldThrowArgumentException()
    {
        // Arrange
        var minorDateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-17));

        // Act & Assert
        var act = () => new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            DateOfBirth = minorDateOfBirth
        };

        act.Should().Throw<ArgumentException>()
           .WithMessage("*User must be at least 18 years old*");
    }

    [Fact]
    public void User_WhenUpdatingProfile_ShouldUpdateLastModifiedAt()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            DateOfBirth = new DateOnly(1995, 5, 15)
        };
        
        var originalModifiedAt = user.LastModifiedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        user.UpdateProfile("Jane", "Smith", "Passionate about hiking and cooking!");

        // Assert
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.Bio.Should().Be("Passionate about hiking and cooking!");
        user.LastModifiedAt.Should().BeAfter(originalModifiedAt);
    }

    [Fact]
    public void User_WhenDeactivating_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            DateOfBirth = new DateOnly(1995, 5, 15)
        };

        user.IsActive.Should().BeTrue(); // Should be active by default

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }
}