using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Matching;

public class CompatibilityAgent : BaseAgent
{
    private readonly IUserRepository _userRepository;
    private readonly IAIService _aiService;
    private readonly ILogger<CompatibilityAgent> _logger;

    public CompatibilityAgent(
        IUserRepository userRepository,
        IAIService aiService,
        ILogger<CompatibilityAgent> logger)
    {
        _userRepository = userRepository;
        _aiService = aiService;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting compatibility analysis");

            // Validate and extract user IDs
            var validationResult = ValidateParameters(request.Parameters);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed: {Error}", validationResult.ErrorMessage);
                return AgentResult.CreateError(validationResult.ErrorMessage ?? "Validation failed");
            }

            var (userId1, userId2) = validationResult.UserIds;

            // Get users from database
            var user1 = await _userRepository.GetByIdAsync(userId1, cancellationToken);
            var user2 = await _userRepository.GetByIdAsync(userId2, cancellationToken);

            if (user1 == null || user2 == null)
            {
                _logger.LogWarning("One or both users not found: {UserId1}, {UserId2}", userId1, userId2);
                return AgentResult.CreateError("One or both users not found");
            }

            // Calculate compatibility score
            _logger.LogInformation("Calculating compatibility between users {UserId1} and {UserId2}", userId1, userId2);
            
            var detailedScore = await CalculateDetailedCompatibilityAsync(user1, user2, cancellationToken);
            var overallScore = detailedScore.OverallScore;

            _logger.LogInformation("Compatibility calculated: {Score}", overallScore);

            // Return results
            return AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["userId1"] = userId1,
                ["userId2"] = userId2,
                ["compatibilityScore"] = overallScore,
                ["detailedScore"] = new Dictionary<string, object>
                {
                    ["interestCompatibility"] = detailedScore.InterestCompatibility,
                    ["personalityCompatibility"] = detailedScore.PersonalityCompatibility,
                    ["lifestyleCompatibility"] = detailedScore.LifestyleCompatibility,
                    ["valueCompatibility"] = detailedScore.ValueCompatibility,
                    ["overallScore"] = detailedScore.OverallScore,
                    ["compatibilityLevel"] = detailedScore.GetCompatibilityLevel()
                },
                ["timestamp"] = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate compatibility");
            return AgentResult.CreateError($"Failed to calculate compatibility: {ex.Message}");
        }
    }

    private async Task<CompatibilityScore> CalculateDetailedCompatibilityAsync(
        User user1, 
        User user2, 
        CancellationToken cancellationToken)
    {
        var score = new CompatibilityScore();

        // Calculate interest compatibility
        score.InterestCompatibility = CalculateInterestCompatibility(user1, user2);

        // Calculate lifestyle compatibility (location, occupation, age preferences)
        score.LifestyleCompatibility = CalculateLifestyleCompatibility(user1, user2);

        // Try to use AI for personality and value compatibility
        try
        {
            var aiScore = await _aiService.CalculateCompatibilityScoreAsync(
                BuildUserProfileText(user1),
                BuildUserProfileText(user2),
                cancellationToken);

            score.PersonalityCompatibility = aiScore;
            score.ValueCompatibility = aiScore; // Use AI score for both initially
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service unavailable, using fallback calculation");
            
            // Fallback: use profile-based calculations
            score.PersonalityCompatibility = CalculateFallbackPersonalityScore(user1, user2);
            score.ValueCompatibility = CalculateFallbackValueScore(user1, user2);
        }

        return score;
    }

    private static int CalculateInterestCompatibility(User user1, User user2)
    {
        if (user1.Profile == null || user2.Profile == null)
            return 50; // Neutral score

        var interests1 = user1.Profile.InterestTags;
        var interests2 = user2.Profile.InterestTags;

        if (!interests1.Any() || !interests2.Any())
            return 50;

        var commonInterests = interests1.Intersect(interests2).Count();
        var totalUniqueInterests = interests1.Union(interests2).Count();

        if (totalUniqueInterests == 0)
            return 50;

        return (int)((double)commonInterests / totalUniqueInterests * 100);
    }

    private static int CalculateLifestyleCompatibility(User user1, User user2)
    {
        if (user1.Profile == null || user2.Profile == null)
            return 50;

        var score = 0;
        var factors = 0;

        // Location compatibility
        if (!string.IsNullOrEmpty(user1.Profile.Location) && !string.IsNullOrEmpty(user2.Profile.Location))
        {
            var locationMatch = string.Equals(
                user1.Profile.Location, 
                user2.Profile.Location, 
                StringComparison.OrdinalIgnoreCase);
            score += locationMatch ? 100 : 30;
            factors++;
        }

        // Age preference compatibility
        var age1 = user1.Age;
        var age2 = user2.Age;
        
        var user1AcceptsUser2Age = 
            (!user1.Profile.MinAge.HasValue || age2 >= user1.Profile.MinAge.Value) &&
            (!user1.Profile.MaxAge.HasValue || age2 <= user1.Profile.MaxAge.Value);
            
        var user2AcceptsUser1Age = 
            (!user2.Profile.MinAge.HasValue || age1 >= user2.Profile.MinAge.Value) &&
            (!user2.Profile.MaxAge.HasValue || age1 <= user2.Profile.MaxAge.Value);

        if (user1AcceptsUser2Age && user2AcceptsUser1Age)
            score += 100;
        else if (user1AcceptsUser2Age || user2AcceptsUser1Age)
            score += 50;
        
        factors++;

        // Gender preference compatibility
        var genderMatch = 
            user1.Profile.InterestedInGenders.Contains(user2.Profile.GenderIdentity) &&
            user2.Profile.InterestedInGenders.Contains(user1.Profile.GenderIdentity);
        
        score += genderMatch ? 100 : 0;
        factors++;

        return factors > 0 ? score / factors : 50;
    }

    private static int CalculateFallbackPersonalityScore(User user1, User user2)
    {
        // Simple heuristic based on bio length and content similarity
        if (string.IsNullOrEmpty(user1.Bio) || string.IsNullOrEmpty(user2.Bio))
            return 60;

        var bio1Words = user1.Bio.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bio2Words = user2.Bio.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var commonWords = bio1Words.Intersect(bio2Words).Count();
        var totalWords = bio1Words.Union(bio2Words).Count();

        if (totalWords == 0)
            return 60;

        return Math.Min(100, 40 + (int)((double)commonWords / totalWords * 60));
    }

    private static int CalculateFallbackValueScore(User user1, User user2)
    {
        // Use occupation as a proxy for values
        if (user1.Profile == null || user2.Profile == null)
            return 65;

        if (string.IsNullOrEmpty(user1.Profile.Occupation) || string.IsNullOrEmpty(user2.Profile.Occupation))
            return 65;

        var sameOccupation = string.Equals(
            user1.Profile.Occupation,
            user2.Profile.Occupation,
            StringComparison.OrdinalIgnoreCase);

        return sameOccupation ? 85 : 65;
    }

    private static string BuildUserProfileText(User user)
    {
        var parts = new List<string>
        {
            $"Name: {user.FullName}",
            $"Age: {user.Age}"
        };

        if (!string.IsNullOrEmpty(user.Bio))
            parts.Add($"Bio: {user.Bio}");

        if (user.Profile != null)
        {
            if (!string.IsNullOrEmpty(user.Profile.Interests))
                parts.Add($"Interests: {user.Profile.Interests}");

            if (!string.IsNullOrEmpty(user.Profile.Occupation))
                parts.Add($"Occupation: {user.Profile.Occupation}");

            if (!string.IsNullOrEmpty(user.Profile.Location))
                parts.Add($"Location: {user.Profile.Location}");
        }

        return string.Join(". ", parts);
    }

    private static ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("userId1", out var userId1Value) || userId1Value == null)
            return ValidationResult.Invalid("userId1 is required");

        if (!parameters.TryGetValue("userId2", out var userId2Value) || userId2Value == null)
            return ValidationResult.Invalid("userId2 is required");

        if (!Guid.TryParse(userId1Value.ToString(), out var userId1))
            return ValidationResult.Invalid("Invalid userId1 format");

        if (!Guid.TryParse(userId2Value.ToString(), out var userId2))
            return ValidationResult.Invalid("Invalid userId2 format");

        if (userId1 == userId2)
            return ValidationResult.Invalid("Cannot calculate compatibility with self");

        return ValidationResult.Valid((userId1, userId2));
    }

    private class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }
        public (Guid UserId1, Guid UserId2) UserIds { get; private set; }

        public static ValidationResult Valid((Guid, Guid) userIds)
        {
            return new ValidationResult
            {
                IsValid = true,
                UserIds = userIds
            };
        }

        public static ValidationResult Invalid(string errorMessage)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
