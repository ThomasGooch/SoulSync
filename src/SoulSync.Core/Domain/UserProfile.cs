using SoulSync.Core.Enums;

namespace SoulSync.Core.Domain;

public class UserProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? Interests { get; set; }
    public string? Location { get; set; }
    public string? Occupation { get; set; }
    public GenderIdentity GenderIdentity { get; set; }
    public List<GenderIdentity> InterestedInGenders { get; set; } = new();
    
    // AI-related properties
    public string? AIInsights { get; private set; }
    public DateTime? AIAnalysisCompletedAt { get; private set; }
    public bool HasAIAnalysis => !string.IsNullOrEmpty(AIInsights);
    
    // Dating preferences
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MaxDistanceKm { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }

    // Computed properties
    public List<string> InterestTags
    {
        get
        {
            if (string.IsNullOrEmpty(Interests))
                return new List<string>();

            return Interests
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim().ToLowerInvariant())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList();
        }
    }

    // Methods
    public void AddAIInsights(string insights)
    {
        if (string.IsNullOrWhiteSpace(insights))
            throw new ArgumentException("AI insights cannot be empty", nameof(insights));

        AIInsights = insights;
        AIAnalysisCompletedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdatePreferences(string? interests, string? location, string? occupation)
    {
        Interests = interests;
        Location = location;
        Occupation = occupation;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateDatingPreferences(int? minAge, int? maxAge, int? maxDistanceKm, List<GenderIdentity>? interestedInGenders)
    {
        MinAge = minAge;
        MaxAge = maxAge;
        MaxDistanceKm = maxDistanceKm;
        
        if (interestedInGenders != null)
            InterestedInGenders = interestedInGenders;
            
        LastModifiedAt = DateTime.UtcNow;
    }

    public int CalculateBasicCompatibilityScore(UserProfile otherProfile)
    {
        if (otherProfile == null)
            return 0;

        var score = 0;
        var factors = 0;

        // Interest compatibility (40% weight)
        var interestScore = CalculateInterestCompatibility(otherProfile);
        score += (int)(interestScore * 0.4);
        factors++;

        // Gender preference compatibility (30% weight)
        var genderScore = CalculateGenderCompatibility(otherProfile);
        score += (int)(genderScore * 0.3);
        factors++;

        // Age preference compatibility (20% weight)
        var ageScore = CalculateAgeCompatibility(otherProfile);
        score += (int)(ageScore * 0.2);
        factors++;

        // Location proximity (10% weight)
        var locationScore = CalculateLocationCompatibility(otherProfile);
        score += (int)(locationScore * 0.1);
        factors++;

        return Math.Min(100, score);
    }

    private int CalculateInterestCompatibility(UserProfile otherProfile)
    {
        var myInterests = InterestTags;
        var otherInterests = otherProfile.InterestTags;

        if (!myInterests.Any() || !otherInterests.Any())
            return 50; // Neutral score if no interests specified

        var commonInterests = myInterests.Intersect(otherInterests).Count();
        var totalUniqueInterests = myInterests.Union(otherInterests).Count();

        if (totalUniqueInterests == 0)
            return 50;

        return (int)((double)commonInterests / totalUniqueInterests * 100);
    }

    private int CalculateGenderCompatibility(UserProfile otherProfile)
    {
        var isInterestedInThem = InterestedInGenders.Contains(otherProfile.GenderIdentity);
        var areTheyInterestedInMe = otherProfile.InterestedInGenders.Contains(GenderIdentity);

        if (isInterestedInThem && areTheyInterestedInMe)
            return 100;
        
        if (isInterestedInThem || areTheyInterestedInMe)
            return 50;
            
        return 0;
    }

    private int CalculateAgeCompatibility(UserProfile otherProfile)
    {
        // For basic compatibility, assume moderate age compatibility
        // This would be enhanced with actual user ages from the User entity
        return 75;
    }

    private int CalculateLocationCompatibility(UserProfile otherProfile)
    {
        if (string.IsNullOrEmpty(Location) || string.IsNullOrEmpty(otherProfile.Location))
            return 50; // Neutral if location not specified

        // Simple string comparison for now - could be enhanced with actual geolocation
        if (string.Equals(Location, otherProfile.Location, StringComparison.OrdinalIgnoreCase))
            return 100;

        return 25; // Low compatibility for different locations
    }
}