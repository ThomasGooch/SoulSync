using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Analysis;

public class ProfileAnalysisAgent : BaseAgent
{
    private readonly IAIService _aiService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProfileAnalysisAgent> _logger;

    public ProfileAnalysisAgent(
        IAIService aiService,
        IUserRepository userRepository,
        ILogger<ProfileAnalysisAgent> logger)
    {
        _aiService = aiService;
        _userRepository = userRepository;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting profile analysis");

            // Validate and extract user ID
            var validationResult = ValidateParameters(request.Parameters);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed: {Error}", validationResult.ErrorMessage);
                return AgentResult.CreateError(validationResult.ErrorMessage);
            }

            var userId = validationResult.UserId;

            // Get user from database
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return AgentResult.CreateError($"User with ID {userId} not found");
            }

            // Build content for analysis
            var contentToAnalyze = BuildAnalysisContent(user);
            if (string.IsNullOrEmpty(contentToAnalyze))
            {
                _logger.LogWarning("No content to analyze for user {UserId}", userId);
                return AgentResult.CreateError("No profile content available for analysis");
            }

            // Perform AI analysis
            _logger.LogInformation("Analyzing profile content for user {UserId}", userId);
            var aiInsights = await _aiService.AnalyzeProfileAsync(contentToAnalyze, cancellationToken);

            // Create or update user profile with AI insights
            if (user.Profile == null)
            {
                user.Profile = new UserProfile { UserId = userId };
            }

            user.Profile.AddAIInsights(aiInsights);

            // Save updated user
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("Profile analysis completed for user {UserId}", userId);

            // Extract personality traits and interest categories for enhanced response
            var personalityTraits = ExtractPersonalityTraits(aiInsights);
            var interestCategories = CategorizeInterests(user);

            // Return analysis results
            return AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["insights"] = aiInsights,
                ["personalityTraits"] = personalityTraits,
                ["interestCategories"] = interestCategories,
                ["analysisCompletedAt"] = user.Profile.AIAnalysisCompletedAt,
                ["hasProfile"] = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze profile");
            return AgentResult.CreateError("Failed to analyze profile: " + ex.Message);
        }
    }

    private static ValidationResult ValidateParameters(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("userId", out var userIdValue) || userIdValue == null)
            return ValidationResult.Invalid("User ID is required");

        if (!Guid.TryParse(userIdValue.ToString(), out var userId))
            return ValidationResult.Invalid("Invalid user ID format");

        return ValidationResult.Valid(userId);
    }

    private static string BuildAnalysisContent(User user)
    {
        var contentParts = new List<string>();

        // Add basic user info
        contentParts.Add($"Name: {user.FullName}");
        contentParts.Add($"Age: {user.Age}");

        // Add bio if available
        if (!string.IsNullOrEmpty(user.Bio))
            contentParts.Add($"Bio: {user.Bio}");

        // Add profile information if available
        if (user.Profile != null)
        {
            if (!string.IsNullOrEmpty(user.Profile.Interests))
                contentParts.Add($"Interests: {user.Profile.Interests}");

            if (!string.IsNullOrEmpty(user.Profile.Occupation))
                contentParts.Add($"Occupation: {user.Profile.Occupation}");

            if (!string.IsNullOrEmpty(user.Profile.Location))
                contentParts.Add($"Location: {user.Profile.Location}");

            contentParts.Add($"Gender Identity: {user.Profile.GenderIdentity}");
        }

        return string.Join(". ", contentParts);
    }

    private static List<string> ExtractPersonalityTraits(string insights)
    {
        var traits = new List<string>();
        var lowerInsights = insights.ToLowerInvariant();

        // Common personality trait keywords
        var traitKeywords = new Dictionary<string, string>
        {
            ["creative"] = "Creative",
            ["artistic"] = "Artistic",
            ["extrovert"] = "Extroverted",
            ["introvert"] = "Introverted",
            ["social"] = "Social",
            ["adventure"] = "Adventurous",
            ["curious"] = "Curious",
            ["intellectual"] = "Intellectual",
            ["analytical"] = "Analytical",
            ["outgoing"] = "Outgoing",
            ["friendly"] = "Friendly",
            ["ambitious"] = "Ambitious",
            ["organized"] = "Organized",
            ["spontaneous"] = "Spontaneous",
            ["empathetic"] = "Empathetic",
            ["leadership"] = "Leader",
            ["independent"] = "Independent",
            ["collaborative"] = "Collaborative"
        };

        foreach (var (keyword, trait) in traitKeywords)
        {
            if (lowerInsights.Contains(keyword))
                traits.Add(trait);
        }

        return traits.Distinct().ToList();
    }

    private static Dictionary<string, List<string>> CategorizeInterests(User user)
    {
        var categories = new Dictionary<string, List<string>>();

        if (user.Profile?.InterestTags == null || !user.Profile.InterestTags.Any())
            return categories;

        // Define interest categories
        var categoryMappings = new Dictionary<string, List<string>>
        {
            ["Sports & Fitness"] = new() { "running", "cycling", "hiking", "swimming", "yoga", "gym", "fitness", "sports", "tennis", "basketball", "soccer", "football" },
            ["Arts & Culture"] = new() { "painting", "music", "art", "theatre", "museums", "galleries", "concerts", "dance", "photography", "writing", "literature" },
            ["Technology"] = new() { "programming", "coding", "tech", "gadgets", "computers", "software", "gaming", "ai", "robotics", "engineering" },
            ["Food & Cooking"] = new() { "cooking", "food", "restaurants", "wine", "coffee", "baking", "culinary", "dining", "cuisine" },
            ["Travel & Adventure"] = new() { "travel", "adventure", "exploring", "backpacking", "camping", "nature", "outdoors", "mountains", "beaches" },
            ["Learning & Education"] = new() { "reading", "books", "learning", "education", "courses", "languages", "history", "science", "research" },
            ["Social & Entertainment"] = new() { "movies", "tv", "shows", "parties", "socializing", "friends", "networking", "events", "festivals" },
            ["Health & Wellness"] = new() { "meditation", "wellness", "health", "mindfulness", "spa", "relaxation", "self-care", "therapy" }
        };

        // Categorize user interests
        foreach (var interest in user.Profile.InterestTags)
        {
            foreach (var (categoryName, keywords) in categoryMappings)
            {
                if (keywords.Any(keyword => interest.Contains(keyword)))
                {
                    if (!categories.ContainsKey(categoryName))
                        categories[categoryName] = new List<string>();
                    
                    categories[categoryName].Add(interest);
                }
            }
        }

        // Add uncategorized interests
        var categorizedInterests = categories.Values.SelectMany(x => x).ToHashSet();
        var uncategorizedInterests = user.Profile.InterestTags
            .Where(interest => !categorizedInterests.Contains(interest))
            .ToList();

        if (uncategorizedInterests.Any())
        {
            categories["Other"] = uncategorizedInterests;
        }

        return categories;
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