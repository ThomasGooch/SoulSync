using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Matching;

public class PreferenceLearningAgent : BaseAgent
{
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PreferenceLearningAgent> _logger;

    public PreferenceLearningAgent(
        IUserPreferencesRepository preferencesRepository,
        IMatchRepository matchRepository,
        IUserRepository userRepository,
        ILogger<PreferenceLearningAgent> logger)
    {
        _preferencesRepository = preferencesRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting preference learning");

            // Validate and extract user ID
            var validationResult = ValidateParameters(request.Parameters);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed: {Error}", validationResult.ErrorMessage);
                return AgentResult.CreateError(validationResult.ErrorMessage ?? "Validation failed");
            }

            var userId = validationResult.UserId;

            // Get or create user preferences
            var preferences = await _preferencesRepository.GetOrCreateAsync(userId, cancellationToken);
            
            _logger.LogInformation("Learning preferences for user {UserId}", userId);

            // Get user's match history
            var matches = await _matchRepository.GetMatchesForUserAsync(userId, cancellationToken);
            var matchList = matches.ToList();

            if (!matchList.Any())
            {
                _logger.LogInformation("No match history found for user {UserId}", userId);
                preferences.RecordLearningSession();
                await _preferencesRepository.UpdateAsync(preferences, cancellationToken);
                
                return AgentResult.CreateSuccess(new Dictionary<string, object>
                {
                    ["userId"] = userId,
                    ["preferencesUpdated"] = true,
                    ["matchesAnalyzed"] = 0,
                    ["message"] = "No match history available for learning"
                });
            }

            // Analyze matches and update preferences
            await AnalyzeMatchHistoryAsync(preferences, matchList, cancellationToken);

            // Record learning session and save
            preferences.RecordLearningSession();
            await _preferencesRepository.UpdateAsync(preferences, cancellationToken);

            _logger.LogInformation("Preferences learned for user {UserId}: {AcceptanceCount} acceptances, {RejectionCount} rejections", 
                userId, preferences.MatchAcceptanceCount, preferences.MatchRejectionCount);

            // Return results
            return AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["preferencesUpdated"] = true,
                ["matchesAnalyzed"] = matchList.Count,
                ["acceptanceRate"] = preferences.GetAcceptanceRate(),
                ["averageAcceptedScore"] = preferences.AverageAcceptedCompatibilityScore,
                ["interestWeights"] = preferences.InterestWeights,
                ["learningSessionCount"] = preferences.LearningSessionCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to learn preferences");
            return AgentResult.CreateError($"Failed to learn preferences: {ex.Message}");
        }
    }

    private async Task AnalyzeMatchHistoryAsync(
        UserPreferences preferences, 
        List<Match> matches, 
        CancellationToken cancellationToken)
    {
        var acceptedMatches = new List<Match>();
        var rejectedMatches = new List<Match>();

        // Categorize matches
        foreach (var match in matches)
        {
            if (match.Status == MatchStatus.Accepted)
            {
                acceptedMatches.Add(match);
                preferences.RecordMatchAcceptance(match.CompatibilityScore);
            }
            else if (match.Status == MatchStatus.Rejected)
            {
                rejectedMatches.Add(match);
                preferences.RecordMatchRejection();
            }
        }

        // Learn interest weights from accepted matches
        if (acceptedMatches.Any())
        {
            await LearnInterestWeightsAsync(preferences, acceptedMatches, cancellationToken);
        }

        // Learn personality trait preferences
        if (acceptedMatches.Any() || rejectedMatches.Any())
        {
            LearnPersonalityPreferences(preferences, acceptedMatches, rejectedMatches);
        }
    }

    private async Task LearnInterestWeightsAsync(
        UserPreferences preferences, 
        List<Match> acceptedMatches, 
        CancellationToken cancellationToken)
    {
        var interestFrequency = new Dictionary<string, int>();
        var totalAccepted = acceptedMatches.Count;

        foreach (var match in acceptedMatches)
        {
            // Get the other user's profile
            var otherUserId = match.GetOtherUserId(preferences.UserId);
            var otherUser = await _userRepository.GetByIdAsync(otherUserId, cancellationToken);

            if (otherUser?.Profile?.InterestTags != null)
            {
                foreach (var interest in otherUser.Profile.InterestTags)
                {
                    if (!interestFrequency.ContainsKey(interest))
                        interestFrequency[interest] = 0;
                    
                    interestFrequency[interest]++;
                }
            }
        }

        // Calculate weights based on frequency
        foreach (var (interest, frequency) in interestFrequency)
        {
            var weight = (double)frequency / totalAccepted;
            preferences.UpdateInterestWeight(interest, Math.Min(weight, 1.0));
        }
    }

    private static void LearnPersonalityPreferences(
        UserPreferences preferences, 
        List<Match> acceptedMatches, 
        List<Match> rejectedMatches)
    {
        // Simple heuristic: higher compatibility scores in accepted matches suggest positive traits
        var avgAcceptedScore = acceptedMatches.Any() 
            ? acceptedMatches.Average(m => m.CompatibilityScore) 
            : 0;

        var avgRejectedScore = rejectedMatches.Any() 
            ? rejectedMatches.Average(m => m.CompatibilityScore) 
            : 0;

        // If user accepts high compatibility matches, they prefer compatible personalities
        if (avgAcceptedScore > 75)
        {
            preferences.UpdatePersonalityTraitPreference("compatible", 0.8);
            preferences.UpdatePersonalityTraitPreference("similar", 0.7);
        }

        // If user rejects low compatibility matches, they avoid incompatible personalities
        if (avgRejectedScore < 60)
        {
            preferences.UpdatePersonalityTraitPreference("incompatible", -0.8);
            preferences.UpdatePersonalityTraitPreference("different", -0.5);
        }
    }

    private static ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("userId", out var userIdValue) || userIdValue == null)
            return ValidationResult.Invalid("userId is required");

        if (!Guid.TryParse(userIdValue.ToString(), out var userId))
            return ValidationResult.Invalid("Invalid userId format");

        return ValidationResult.Valid(userId);
    }

    private class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }
        public Guid UserId { get; private set; }

        public static ValidationResult Valid(Guid userId)
        {
            return new ValidationResult
            {
                IsValid = true,
                UserId = userId
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
