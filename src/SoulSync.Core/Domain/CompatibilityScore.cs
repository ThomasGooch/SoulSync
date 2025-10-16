namespace SoulSync.Core.Domain;

public class CompatibilityScore
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MatchId { get; set; }
    
    // Core compatibility factors
    public int InterestCompatibility { get; set; }
    public int PersonalityCompatibility { get; set; }
    public int LifestyleCompatibility { get; set; }
    public int ValueCompatibility { get; set; }
    
    // Additional factors
    public Dictionary<string, int> FactorScores { get; set; } = new();
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public Match? Match { get; set; }

    // Computed property for overall score
    public int OverallScore
    {
        get
        {
            // Weighted average of core factors
            var weights = new Dictionary<string, double>
            {
                ["Interest"] = 0.30,
                ["Personality"] = 0.30,
                ["Lifestyle"] = 0.25,
                ["Value"] = 0.15
            };

            var weightedSum = 
                (InterestCompatibility * weights["Interest"]) +
                (PersonalityCompatibility * weights["Personality"]) +
                (LifestyleCompatibility * weights["Lifestyle"]) +
                (ValueCompatibility * weights["Value"]);

            return (int)Math.Round(weightedSum);
        }
    }

    public void AddFactorScore(string factorName, int score)
    {
        if (score < 0 || score > 100)
            throw new ArgumentException("Factor score must be between 0 and 100", nameof(score));

        FactorScores[factorName] = score;
        LastModifiedAt = DateTime.UtcNow;
    }

    public string GetCompatibilityLevel()
    {
        var score = OverallScore;
        
        return score switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            _ => "Low"
        };
    }

    public void UpdateCoreFactors(int interest, int personality, int lifestyle, int value)
    {
        ValidateScore(interest, nameof(interest));
        ValidateScore(personality, nameof(personality));
        ValidateScore(lifestyle, nameof(lifestyle));
        ValidateScore(value, nameof(value));

        InterestCompatibility = interest;
        PersonalityCompatibility = personality;
        LifestyleCompatibility = lifestyle;
        ValueCompatibility = value;
        LastModifiedAt = DateTime.UtcNow;
    }

    private static void ValidateScore(int score, string paramName)
    {
        if (score < 0 || score > 100)
            throw new ArgumentException($"Score must be between 0 and 100", paramName);
    }
}
