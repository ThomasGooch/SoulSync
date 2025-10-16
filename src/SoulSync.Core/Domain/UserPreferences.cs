namespace SoulSync.Core.Domain;

public class UserPreferences
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    
    // Learned preferences from user interactions
    public Dictionary<string, double> InterestWeights { get; set; } = new();
    public Dictionary<string, double> PersonalityTraitPreferences { get; set; } = new();
    
    // Behavioral data
    public int ProfileViewCount { get; set; }
    public int MatchAcceptanceCount { get; set; }
    public int MatchRejectionCount { get; set; }
    public double AverageAcceptedCompatibilityScore { get; set; }
    
    // Preference learning metadata
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLearningSessionAt { get; private set; }
    public int LearningSessionCount { get; private set; }

    // Navigation properties
    public User? User { get; set; }

    public void RecordMatchAcceptance(int compatibilityScore)
    {
        MatchAcceptanceCount++;
        UpdateAverageScore(compatibilityScore);
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void RecordMatchRejection()
    {
        MatchRejectionCount++;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void RecordProfileView()
    {
        ProfileViewCount++;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInterestWeight(string interest, double weight)
    {
        if (weight < 0 || weight > 1)
            throw new ArgumentException("Weight must be between 0 and 1", nameof(weight));

        InterestWeights[interest.ToLowerInvariant()] = weight;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePersonalityTraitPreference(string trait, double preference)
    {
        if (preference < -1 || preference > 1)
            throw new ArgumentException("Preference must be between -1 and 1", nameof(preference));

        PersonalityTraitPreferences[trait.ToLowerInvariant()] = preference;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void RecordLearningSession()
    {
        LearningSessionCount++;
        LastLearningSessionAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public double GetAcceptanceRate()
    {
        var totalMatches = MatchAcceptanceCount + MatchRejectionCount;
        if (totalMatches == 0)
            return 0;

        return (double)MatchAcceptanceCount / totalMatches;
    }

    private void UpdateAverageScore(int newScore)
    {
        if (MatchAcceptanceCount == 1)
        {
            AverageAcceptedCompatibilityScore = newScore;
        }
        else
        {
            var previousTotal = AverageAcceptedCompatibilityScore * (MatchAcceptanceCount - 1);
            AverageAcceptedCompatibilityScore = (previousTotal + newScore) / MatchAcceptanceCount;
        }
    }
}
