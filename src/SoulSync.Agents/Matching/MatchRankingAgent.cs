using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Matching;

public class MatchRankingAgent : BaseAgent
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly CompatibilityAgent _compatibilityAgent;
    private readonly ILogger<MatchRankingAgent> _logger;

    public MatchRankingAgent(
        IUserRepository userRepository,
        IUserPreferencesRepository preferencesRepository,
        CompatibilityAgent compatibilityAgent,
        ILogger<MatchRankingAgent> logger)
    {
        _userRepository = userRepository;
        _preferencesRepository = preferencesRepository;
        _compatibilityAgent = compatibilityAgent;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting match ranking");

            // Validate and extract parameters
            var validationResult = ValidateParameters(request.Parameters);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed: {Error}", validationResult.ErrorMessage);
                return AgentResult.CreateError(validationResult.ErrorMessage ?? "Validation failed");
            }

            var (userId, maxResults) = validationResult.Data;

            // Get current user
            var currentUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (currentUser == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return AgentResult.CreateError($"User with ID {userId} not found");
            }

            _logger.LogInformation("Ranking matches for user {UserId}", userId);

            // Get potential matches
            var potentialMatches = await _userRepository.GetPotentialMatchesAsync(
                userId, 
                maxResults * 2, // Get more than needed for filtering
                cancellationToken);
            
            var candidatesList = potentialMatches.ToList();
            
            if (!candidatesList.Any())
            {
                _logger.LogInformation("No potential matches found for user {UserId}", userId);
                return AgentResult.CreateSuccess(new Dictionary<string, object>
                {
                    ["userId"] = userId,
                    ["rankedMatches"] = new List<Dictionary<string, object>>(),
                    ["totalCandidates"] = 0
                });
            }

            // Get user preferences if available
            var preferences = await _preferencesRepository.GetByUserIdAsync(userId, cancellationToken);
            var hasPreferences = preferences != null;

            // Calculate compatibility scores for all candidates
            var rankedCandidates = new List<RankedMatch>();
            
            foreach (var candidate in candidatesList)
            {
                var compatibilityRequest = new AgentRequest
                {
                    Parameters = new Dictionary<string, object>
                    {
                        ["userId1"] = userId.ToString(),
                        ["userId2"] = candidate.Id.ToString()
                    }
                };

                var compatibilityResult = await _compatibilityAgent.ExecuteAsync(
                    compatibilityRequest, 
                    cancellationToken);

                if (compatibilityResult.IsSuccess && compatibilityResult.Data is Dictionary<string, object> scoreData)
                {
                    var baseScore = Convert.ToInt32(scoreData["compatibilityScore"]);
                    
                    // Apply preference weighting if available
                    var finalScore = hasPreferences 
                        ? ApplyPreferenceWeighting(baseScore, candidate, preferences!) 
                        : baseScore;

                    rankedCandidates.Add(new RankedMatch
                    {
                        UserId = candidate.Id,
                        User = candidate,
                        BaseCompatibilityScore = baseScore,
                        AdjustedScore = finalScore,
                        DetailedScore = scoreData.ContainsKey("detailedScore") 
                            ? scoreData["detailedScore"] as Dictionary<string, object>
                            : null
                    });
                }
            }

            // Sort by adjusted score and limit results
            var topMatches = rankedCandidates
                .OrderByDescending(m => m.AdjustedScore)
                .ThenByDescending(m => m.BaseCompatibilityScore)
                .Take(maxResults)
                .ToList();

            _logger.LogInformation("Ranked {Count} matches for user {UserId}", topMatches.Count, userId);

            // Build response
            var rankedMatchesData = topMatches.Select(m => new Dictionary<string, object>
            {
                ["userId"] = m.UserId,
                ["userName"] = m.User?.FullName ?? "Unknown",
                ["age"] = m.User?.Age ?? 0,
                ["location"] = m.User?.Profile?.Location ?? "Unknown",
                ["bio"] = m.User?.Bio ?? "",
                ["interests"] = m.User?.Profile?.Interests ?? "",
                ["compatibilityScore"] = m.BaseCompatibilityScore,
                ["adjustedScore"] = m.AdjustedScore,
                ["scoreBoost"] = m.AdjustedScore - m.BaseCompatibilityScore,
                ["detailedScore"] = m.DetailedScore
            }).ToList();

            return AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["rankedMatches"] = rankedMatchesData,
                ["totalCandidates"] = candidatesList.Count,
                ["preferencesApplied"] = hasPreferences,
                ["timestamp"] = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rank matches");
            return AgentResult.CreateError($"Failed to rank matches: {ex.Message}");
        }
    }

    private static int ApplyPreferenceWeighting(
        int baseScore, 
        User candidate, 
        UserPreferences preferences)
    {
        var boost = 0.0;
        var boostCount = 0;

        // Apply interest weight boost
        if (candidate.Profile?.InterestTags != null && preferences.InterestWeights.Any())
        {
            foreach (var interest in candidate.Profile.InterestTags)
            {
                if (preferences.InterestWeights.TryGetValue(interest, out var weight))
                {
                    boost += weight * 10; // Each weighted interest can boost up to 10 points
                    boostCount++;
                }
            }
        }

        // Apply personality trait boost (simplified)
        if (preferences.AverageAcceptedCompatibilityScore > 0)
        {
            // If this match's score is close to user's average accepted score, give a small boost
            var scoreDifference = Math.Abs(baseScore - preferences.AverageAcceptedCompatibilityScore);
            if (scoreDifference < 10)
            {
                boost += 5; // Small boost for matches similar to previously accepted ones
            }
        }

        // Normalize boost
        if (boostCount > 0)
        {
            boost = boost / boostCount; // Average boost from interests
        }

        // Cap the total boost at 15 points
        boost = Math.Min(boost, 15);

        return Math.Min(100, baseScore + (int)Math.Round(boost));
    }

    private static ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("userId", out var userIdValue) || userIdValue == null)
            return ValidationResult.Invalid("userId is required");

        if (!Guid.TryParse(userIdValue.ToString(), out var userId))
            return ValidationResult.Invalid("Invalid userId format");

        // maxResults is optional, default to 10
        var maxResults = 10;
        if (parameters.TryGetValue("maxResults", out var maxResultsValue) && maxResultsValue != null)
        {
            if (!int.TryParse(maxResultsValue.ToString(), out maxResults) || maxResults < 1 || maxResults > 100)
                return ValidationResult.Invalid("maxResults must be between 1 and 100");
        }

        return ValidationResult.Valid((userId, maxResults));
    }

    private class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }
        public (Guid UserId, int MaxResults) Data { get; private set; }

        public static ValidationResult Valid((Guid, int) data)
        {
            return new ValidationResult
            {
                IsValid = true,
                Data = data
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

    private class RankedMatch
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public int BaseCompatibilityScore { get; set; }
        public int AdjustedScore { get; set; }
        public Dictionary<string, object>? DetailedScore { get; set; }
    }
}
