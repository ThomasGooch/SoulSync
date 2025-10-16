namespace SoulSync.Core.Domain;

/// <summary>
/// Represents an AI-generated date suggestion for a matched couple
/// </summary>
public class DateSuggestion
{
    /// <summary>
    /// Unique identifier for the date suggestion
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// The match this suggestion is for
    /// </summary>
    public Guid MatchId { get; set; }
    
    /// <summary>
    /// Title of the date suggestion
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the date activity
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Location/venue for the date
    /// </summary>
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// Category of the date (e.g., "dining", "entertainment", "outdoor", "cultural")
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Estimated cost level (e.g., "$", "$$", "$$$")
    /// </summary>
    public string EstimatedCost { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the suggestion has been accepted
    /// </summary>
    public bool IsAccepted { get; private set; }
    
    /// <summary>
    /// Whether the suggestion has been rejected
    /// </summary>
    public bool IsRejected { get; private set; }
    
    /// <summary>
    /// Whether the date was completed
    /// </summary>
    public bool IsCompleted { get; private set; }
    
    /// <summary>
    /// When the suggestion was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the suggestion was accepted
    /// </summary>
    public DateTime? AcceptedAt { get; private set; }
    
    /// <summary>
    /// When the suggestion was rejected
    /// </summary>
    public DateTime? RejectedAt { get; private set; }
    
    /// <summary>
    /// Reason for rejection (if applicable)
    /// </summary>
    public string? RejectionReason { get; private set; }
    
    /// <summary>
    /// When the date is scheduled for
    /// </summary>
    public DateTime? ScheduledDate { get; private set; }
    
    /// <summary>
    /// When the date was completed
    /// </summary>
    public DateTime? CompletedAt { get; private set; }
    
    /// <summary>
    /// Rating of the date after completion (1-5 stars)
    /// </summary>
    public int? Rating { get; private set; }
    
    /// <summary>
    /// AI-generated reasoning for this suggestion
    /// </summary>
    public string? AIReasoning { get; set; }
    
    /// <summary>
    /// Navigation property to Match
    /// </summary>
    public Match? Match { get; set; }
    
    /// <summary>
    /// Accepts the date suggestion
    /// </summary>
    public void Accept()
    {
        IsAccepted = true;
        IsRejected = false;
        AcceptedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Rejects the date suggestion
    /// </summary>
    /// <param name="reason">Optional reason for rejection</param>
    public void Reject(string? reason = null)
    {
        IsRejected = true;
        IsAccepted = false;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = reason;
    }
    
    /// <summary>
    /// Schedules the date for a specific time
    /// </summary>
    /// <param name="scheduledDate">When the date is scheduled for</param>
    /// <exception cref="InvalidOperationException">Thrown when trying to schedule a non-accepted suggestion</exception>
    public void Schedule(DateTime scheduledDate)
    {
        if (!IsAccepted)
        {
            throw new InvalidOperationException("Date suggestion cannot be scheduled without being accepted first");
        }
        
        ScheduledDate = scheduledDate;
    }
    
    /// <summary>
    /// Marks the date as completed with a rating
    /// </summary>
    /// <param name="rating">Rating from 1-5 stars</param>
    /// <exception cref="InvalidOperationException">Thrown when trying to complete a non-accepted suggestion</exception>
    /// <exception cref="ArgumentException">Thrown when rating is not between 1 and 5</exception>
    public void Complete(int rating)
    {
        if (!IsAccepted)
        {
            throw new InvalidOperationException("Date suggestion cannot be completed without being accepted first");
        }
        
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));
        }
        
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        Rating = rating;
    }
    
    /// <summary>
    /// Gets the current status of the date suggestion
    /// </summary>
    /// <returns>String representation of the current status</returns>
    public string GetStatus()
    {
        if (IsCompleted)
        {
            return "Completed";
        }
        
        if (IsRejected)
        {
            return "Rejected";
        }
        
        if (ScheduledDate.HasValue && IsAccepted)
        {
            return "Scheduled";
        }
        
        if (IsAccepted)
        {
            return "Accepted";
        }
        
        return "Pending";
    }
}
