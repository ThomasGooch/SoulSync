using FluentAssertions;
using SoulSync.Core.Domain;
using Xunit;

namespace SoulSync.Core.Tests.Domain;

public class DateSuggestionTests
{
    [Fact]
    public void DateSuggestion_WhenCreatedWithValidData_ShouldHaveCorrectProperties()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var title = "Coffee at Central Perk";
        var description = "A cozy coffee shop perfect for getting to know each other";
        var location = "Manhattan, NY";
        
        // Act
        var suggestion = new DateSuggestion
        {
            MatchId = matchId,
            Title = title,
            Description = description,
            Location = location
        };
        
        // Assert
        suggestion.Id.Should().NotBeEmpty();
        suggestion.MatchId.Should().Be(matchId);
        suggestion.Title.Should().Be(title);
        suggestion.Description.Should().Be(description);
        suggestion.Location.Should().Be(location);
        suggestion.IsAccepted.Should().BeFalse();
        suggestion.IsRejected.Should().BeFalse();
        suggestion.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void DateSuggestion_WhenAccepted_ShouldUpdateAcceptanceStatus()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Movie Night",
            Description = "Watch the latest blockbuster",
            Location = "AMC Theater"
        };
        
        // Act
        suggestion.Accept();
        
        // Assert
        suggestion.IsAccepted.Should().BeTrue();
        suggestion.IsRejected.Should().BeFalse();
        suggestion.AcceptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void DateSuggestion_WhenRejected_ShouldUpdateRejectionStatus()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Hiking",
            Description = "Mountain trail adventure",
            Location = "State Park"
        };
        
        // Act
        suggestion.Reject("Not interested in outdoor activities");
        
        // Assert
        suggestion.IsRejected.Should().BeTrue();
        suggestion.IsAccepted.Should().BeFalse();
        suggestion.RejectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        suggestion.RejectionReason.Should().Be("Not interested in outdoor activities");
    }
    
    [Fact]
    public void DateSuggestion_WhenCompleted_ShouldUpdateCompletionStatus()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Dinner Date",
            Description = "Fine dining experience",
            Location = "Le Bernardin"
        };
        suggestion.Accept();
        
        // Act
        suggestion.Complete(5);
        
        // Assert
        suggestion.IsCompleted.Should().BeTrue();
        suggestion.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        suggestion.Rating.Should().Be(5);
    }
    
    [Fact]
    public void DateSuggestion_WhenCompletingWithoutAcceptance_ShouldThrowException()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Concert",
            Description = "Live music show",
            Location = "Madison Square Garden"
        };
        
        // Act & Assert
        var act = () => suggestion.Complete(4);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be completed without being accepted*");
    }
    
    [Fact]
    public void DateSuggestion_WhenCompletingWithInvalidRating_ShouldThrowException()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Museum Visit",
            Description = "Art gallery tour",
            Location = "MoMA"
        };
        suggestion.Accept();
        
        // Act & Assert
        var actLow = () => suggestion.Complete(0);
        actLow.Should().Throw<ArgumentException>()
            .WithMessage("*rating must be between 1 and 5*");
        
        var actHigh = () => suggestion.Complete(6);
        actHigh.Should().Throw<ArgumentException>()
            .WithMessage("*rating must be between 1 and 5*");
    }
    
    [Fact]
    public void DateSuggestion_WhenScheduling_ShouldSetScheduledDate()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Brunch",
            Description = "Sunday brunch",
            Location = "Café Mogador"
        };
        suggestion.Accept();
        var scheduledDate = DateTime.UtcNow.AddDays(7);
        
        // Act
        suggestion.Schedule(scheduledDate);
        
        // Assert
        suggestion.ScheduledDate.Should().BeCloseTo(scheduledDate, TimeSpan.FromSeconds(1));
    }
    
    [Fact]
    public void DateSuggestion_WhenSchedulingWithoutAcceptance_ShouldThrowException()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Theater",
            Description = "Broadway show",
            Location = "Times Square"
        };
        
        // Act & Assert
        var act = () => suggestion.Schedule(DateTime.UtcNow.AddDays(7));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be scheduled without being accepted*");
    }
    
    [Fact]
    public void DateSuggestion_GetStatus_ShouldReturnCorrectStatus()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Park Picnic",
            Description = "Outdoor lunch",
            Location = "Central Park"
        };
        
        // Act & Assert - Pending
        suggestion.GetStatus().Should().Be("Pending");
        
        // Accepted
        suggestion.Accept();
        suggestion.GetStatus().Should().Be("Accepted");
        
        // Scheduled
        suggestion.Schedule(DateTime.UtcNow.AddDays(5));
        suggestion.GetStatus().Should().Be("Scheduled");
        
        // Completed
        suggestion.Complete(4);
        suggestion.GetStatus().Should().Be("Completed");
    }
    
    [Fact]
    public void DateSuggestion_GetStatus_WhenRejected_ShouldReturnRejected()
    {
        // Arrange
        var suggestion = new DateSuggestion
        {
            MatchId = Guid.NewGuid(),
            Title = "Karaoke",
            Description = "Singing night",
            Location = "Karaoke Bar"
        };
        
        // Act
        suggestion.Reject("Too shy for karaoke");
        
        // Assert
        suggestion.GetStatus().Should().Be("Rejected");
    }
}
