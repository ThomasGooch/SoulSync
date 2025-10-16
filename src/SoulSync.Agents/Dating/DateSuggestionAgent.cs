using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;
using System.Text.RegularExpressions;

namespace SoulSync.Agents.Dating;

/// <summary>
/// Agent responsible for generating AI-powered date suggestions based on user profiles and shared interests
/// </summary>
public class DateSuggestionAgent : BaseAgent
{
    private readonly IDateSuggestionRepository _dateSuggestionRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAIService _aiService;
    private readonly ILogger<DateSuggestionAgent> _logger;
    
    public DateSuggestionAgent(
        IDateSuggestionRepository dateSuggestionRepository,
        IMatchRepository matchRepository,
        IUserRepository userRepository,
        IAIService aiService,
        ILogger<DateSuggestionAgent> logger)
    {
        _dateSuggestionRepository = dateSuggestionRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
        _aiService = aiService;
        _logger = logger;
    }
    
    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request, 
        CancellationToken cancellationToken)
    {
        // Get the action to perform
        if (!request.Parameters.TryGetValue("action", out var actionObj) || actionObj is not string action)
        {
            return AgentResult.CreateError("Missing required parameter: action");
        }
        
        // Route to appropriate handler based on action
        return action.ToLower() switch
        {
            "generate" => await GenerateSuggestionsAsync(request.Parameters, cancellationToken),
            "accept" => await AcceptSuggestionAsync(request.Parameters, cancellationToken),
            "reject" => await RejectSuggestionAsync(request.Parameters, cancellationToken),
            "schedule" => await ScheduleSuggestionAsync(request.Parameters, cancellationToken),
            "complete" => await CompleteSuggestionAsync(request.Parameters, cancellationToken),
            "get" => await GetSuggestionsAsync(request.Parameters, cancellationToken),
            _ => AgentResult.CreateError($"Invalid action: {action}. Valid actions are: generate, accept, reject, schedule, complete, get")
        };
    }
    
    private async Task<AgentResult> GenerateSuggestionsAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        // Validate required parameters
        if (!parameters.TryGetValue("matchId", out var matchIdObj) || !Guid.TryParse(matchIdObj.ToString(), out var matchId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: matchId");
        }
        
        // Get number of suggestions to generate (default to 3)
        var count = 3;
        if (parameters.TryGetValue("count", out var countObj) && int.TryParse(countObj.ToString(), out var requestedCount))
        {
            count = Math.Min(requestedCount, 10); // Cap at 10 suggestions
        }
        
        // Get match
        var match = await _matchRepository.GetByIdAsync(matchId, cancellationToken);
        if (match == null)
        {
            return AgentResult.CreateError($"Match not found with ID: {matchId}");
        }
        
        // Get both users
        var user1 = await _userRepository.GetByIdAsync(match.UserId1, cancellationToken);
        var user2 = await _userRepository.GetByIdAsync(match.UserId2, cancellationToken);
        
        if (user1 == null || user2 == null)
        {
            return AgentResult.CreateError("Unable to retrieve user profiles for match");
        }
        
        // Generate suggestions using AI
        List<DateSuggestion> suggestions;
        
        try
        {
            suggestions = await GenerateAISuggestionsAsync(match, user1, user2, count, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service failed, using fallback suggestions");
            suggestions = GenerateFallbackSuggestions(match, user1, user2, count);
        }
        
        // Save suggestions
        var createdSuggestions = new List<DateSuggestion>();
        foreach (var suggestion in suggestions)
        {
            var created = await _dateSuggestionRepository.CreateAsync(suggestion, cancellationToken);
            createdSuggestions.Add(created);
        }
        
        _logger.LogInformation("Generated {Count} date suggestions for match {MatchId}", createdSuggestions.Count, matchId);
        
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["suggestions"] = createdSuggestions.Select(s => new Dictionary<string, object>
            {
                ["id"] = s.Id,
                ["title"] = s.Title,
                ["description"] = s.Description,
                ["location"] = s.Location,
                ["category"] = s.Category,
                ["estimatedCost"] = s.EstimatedCost,
                ["status"] = s.GetStatus()
            }).ToList()
        });
    }
    
    private async Task<List<DateSuggestion>> GenerateAISuggestionsAsync(
        Core.Domain.Match match, 
        User user1, 
        User user2, 
        int count, 
        CancellationToken cancellationToken)
    {
        // Build prompt for AI
        var prompt = $@"Generate {count} personalized date suggestions for a matched couple on a dating app.

User 1: {user1.FirstName}
Interests: {user1.Profile?.Interests ?? "general activities"}

User 2: {user2.FirstName}
Interests: {user2.Profile?.Interests ?? "general activities"}

Compatibility Score: {match.CompatibilityScore}%

Please provide {count} creative, specific date ideas that align with their shared interests. 
For each suggestion, provide:
1. A catchy title
2. A brief description
3. A specific location or venue type
4. Category (dining/entertainment/outdoor/cultural/adventure)
5. Estimated cost level ($, $$, or $$$)

Format each suggestion as:
Title: [Title]
Description: [Description]
Location: [Location]
Category: [Category]
Cost: [Cost Level]";

        var aiResponse = await _aiService.ProcessRequestAsync(prompt, cancellationToken);
        
        return ParseAISuggestions(aiResponse, match.Id);
    }
    
    private List<DateSuggestion> ParseAISuggestions(string aiResponse, Guid matchId)
    {
        var suggestions = new List<DateSuggestion>();
        
        // Parse AI response to extract suggestions
        var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        DateSuggestion? current = null;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
            {
                if (current != null)
                {
                    suggestions.Add(current);
                }
                current = new DateSuggestion
                {
                    MatchId = matchId,
                    Title = trimmedLine.Substring(6).Trim()
                };
            }
            else if (current != null)
            {
                if (trimmedLine.StartsWith("Description:", StringComparison.OrdinalIgnoreCase))
                {
                    current.Description = trimmedLine.Substring(12).Trim();
                }
                else if (trimmedLine.StartsWith("Location:", StringComparison.OrdinalIgnoreCase))
                {
                    current.Location = trimmedLine.Substring(9).Trim();
                }
                else if (trimmedLine.StartsWith("Category:", StringComparison.OrdinalIgnoreCase))
                {
                    current.Category = trimmedLine.Substring(9).Trim();
                }
                else if (trimmedLine.StartsWith("Cost:", StringComparison.OrdinalIgnoreCase))
                {
                    current.EstimatedCost = trimmedLine.Substring(5).Trim();
                }
            }
            // Also handle numbered format like "1. Coffee Date at..."
            else if (Regex.IsMatch(trimmedLine, @"^\d+\.\s+(.+)"))
            {
                if (current != null)
                {
                    suggestions.Add(current);
                }
                var regexMatch = Regex.Match(trimmedLine, @"^\d+\.\s+(.+?)(?:\s+-\s+(.+))?$");
                current = new DateSuggestion
                {
                    MatchId = matchId,
                    Title = regexMatch.Groups[1].Value.Trim(),
                    Description = regexMatch.Groups.Count > 2 ? regexMatch.Groups[2].Value.Trim() : "",
                    Location = "TBD",
                    Category = "general",
                    EstimatedCost = "$$"
                };
            }
        }
        
        if (current != null)
        {
            suggestions.Add(current);
        }
        
        // Ensure we have at least one suggestion
        if (suggestions.Count == 0)
        {
            suggestions.Add(new DateSuggestion
            {
                MatchId = matchId,
                Title = "Coffee and Conversation",
                Description = "Meet at a cozy local coffee shop for a relaxed first date",
                Location = "Local Coffee Shop",
                Category = "dining",
                EstimatedCost = "$"
            });
        }
        
        return suggestions;
    }
    
    private List<DateSuggestion> GenerateFallbackSuggestions(Core.Domain.Match match, User user1, User user2, int count)
    {
        // Generate thoughtful suggestions based on user profiles and common interests
        var suggestions = new List<DateSuggestion>();
        
        // Get common interests from both users
        var user1Interests = user1.Profile?.InterestTags ?? new List<string>();
        var user2Interests = user2.Profile?.InterestTags ?? new List<string>();
        var commonInterests = user1Interests.Intersect(user2Interests).ToList();
        
        var fallbackOptions = new List<(string Title, string Description, string Location, string Category, string Cost, List<string> Keywords)>
        {
            ("Coffee Date", "Start with a casual coffee to get to know each other", "Local Coffee Shop", "dining", "$", new List<string> { "coffee", "casual", "conversation" }),
            ("Dinner and Conversation", "Enjoy a nice dinner at a restaurant", "Downtown Restaurant", "dining", "$$", new List<string> { "food", "dining", "restaurant" }),
            ("Museum Visit", "Explore art and culture together", "Local Museum", "cultural", "$$", new List<string> { "art", "culture", "museum", "history" }),
            ("Park Walk", "Take a relaxing walk in a scenic park", "City Park", "outdoor", "$", new List<string> { "outdoor", "nature", "walking", "park" }),
            ("Movie Night", "Catch the latest film together", "Movie Theater", "entertainment", "$$", new List<string> { "movies", "film", "cinema", "entertainment" }),
            ("Live Music", "Enjoy live music at a local venue", "Music Venue", "entertainment", "$$$", new List<string> { "music", "concert", "live", "entertainment" }),
            ("Cooking Class", "Learn to make a new dish together", "Cooking School", "activity", "$$$", new List<string> { "cooking", "food", "learning", "class" }),
            ("Picnic", "Outdoor picnic with homemade treats", "Scenic Park", "outdoor", "$", new List<string> { "outdoor", "nature", "picnic", "food" }),
            ("Art Gallery", "Browse contemporary art exhibits", "Art Gallery", "cultural", "$", new List<string> { "art", "culture", "gallery", "creative" }),
            ("Wine Tasting", "Sample local wines", "Winery", "dining", "$$$", new List<string> { "wine", "tasting", "drinks", "food" }),
            ("Hiking Adventure", "Explore nature trails together", "Nature Trail", "outdoor", "$", new List<string> { "hiking", "outdoor", "nature", "adventure", "sports" }),
            ("Game Night", "Play board games or video games together", "Gaming Cafe", "entertainment", "$", new List<string> { "games", "gaming", "fun", "entertainment" }),
            ("Bookstore Browse", "Explore a local bookstore and share favorite reads", "Independent Bookstore", "cultural", "$", new List<string> { "books", "reading", "literature", "culture" }),
            ("Dance Class", "Learn a new dance style together", "Dance Studio", "activity", "$$", new List<string> { "dance", "dancing", "music", "activity" }),
            ("Sports Event", "Watch a live sports game together", "Sports Arena", "entertainment", "$$$", new List<string> { "sports", "game", "athletic", "entertainment" })
        };
        
        // Score suggestions based on interest matching
        var scoredOptions = fallbackOptions.Select(option => new
        {
            Option = option,
            Score = option.Keywords.Count(k => commonInterests.Any(ci => ci.Contains(k) || k.Contains(ci)))
        }).ToList();
        
        // If we have common interests, prioritize matching suggestions
        IEnumerable<(string Title, string Description, string Location, string Category, string Cost, List<string> Keywords)> selected;
        if (commonInterests.Any())
        {
            // Sort by score (descending) and take top matches, then randomly select from remaining if needed
            selected = scoredOptions
                .OrderByDescending(x => x.Score)
                .ThenBy(x => Guid.NewGuid()) // Random tiebreaker
                .Take(count)
                .Select(x => x.Option);
        }
        else
        {
            // No common interests, use variety - select diverse categories
            var random = new Random();
            selected = fallbackOptions
                .OrderBy(x => random.Next())
                .Take(count);
        }
        
        foreach (var option in selected)
        {
            suggestions.Add(new DateSuggestion
            {
                MatchId = match.Id,
                Title = option.Title,
                Description = option.Description,
                Location = option.Location,
                Category = option.Category,
                EstimatedCost = option.Cost
            });
        }
        
        return suggestions;
    }
    
    private async Task<AgentResult> AcceptSuggestionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("suggestionId", out var suggestionIdObj) || !Guid.TryParse(suggestionIdObj.ToString(), out var suggestionId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: suggestionId");
        }
        
        var suggestion = await _dateSuggestionRepository.GetByIdAsync(suggestionId, cancellationToken);
        if (suggestion == null)
        {
            return AgentResult.CreateError($"Date suggestion not found with ID: {suggestionId}");
        }
        
        suggestion.Accept();
        
        var updated = await _dateSuggestionRepository.UpdateAsync(suggestion, cancellationToken);
        
        _logger.LogInformation("Date suggestion {SuggestionId} accepted", suggestionId);
        
        return BuildSuggestionResult(updated);
    }
    
    private async Task<AgentResult> RejectSuggestionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("suggestionId", out var suggestionIdObj) || !Guid.TryParse(suggestionIdObj.ToString(), out var suggestionId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: suggestionId");
        }
        
        var suggestion = await _dateSuggestionRepository.GetByIdAsync(suggestionId, cancellationToken);
        if (suggestion == null)
        {
            return AgentResult.CreateError($"Date suggestion not found with ID: {suggestionId}");
        }
        
        var reason = parameters.TryGetValue("reason", out var reasonObj) ? reasonObj.ToString() : null;
        
        suggestion.Reject(reason);
        
        var updated = await _dateSuggestionRepository.UpdateAsync(suggestion, cancellationToken);
        
        _logger.LogInformation("Date suggestion {SuggestionId} rejected", suggestionId);
        
        return BuildSuggestionResult(updated);
    }
    
    private async Task<AgentResult> ScheduleSuggestionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("suggestionId", out var suggestionIdObj) || !Guid.TryParse(suggestionIdObj.ToString(), out var suggestionId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: suggestionId");
        }
        
        if (!parameters.TryGetValue("scheduledDate", out var scheduledDateObj) || !DateTime.TryParse(scheduledDateObj.ToString(), out var scheduledDate))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: scheduledDate");
        }
        
        var suggestion = await _dateSuggestionRepository.GetByIdAsync(suggestionId, cancellationToken);
        if (suggestion == null)
        {
            return AgentResult.CreateError($"Date suggestion not found with ID: {suggestionId}");
        }
        
        try
        {
            suggestion.Schedule(scheduledDate);
            
            var updated = await _dateSuggestionRepository.UpdateAsync(suggestion, cancellationToken);
            
            _logger.LogInformation("Date suggestion {SuggestionId} scheduled for {ScheduledDate}", suggestionId, scheduledDate);
            
            return BuildSuggestionResult(updated);
        }
        catch (InvalidOperationException ex)
        {
            return AgentResult.CreateError(ex.Message);
        }
    }
    
    private async Task<AgentResult> CompleteSuggestionAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("suggestionId", out var suggestionIdObj) || !Guid.TryParse(suggestionIdObj.ToString(), out var suggestionId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: suggestionId");
        }
        
        if (!parameters.TryGetValue("rating", out var ratingObj) || !int.TryParse(ratingObj.ToString(), out var rating))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: rating");
        }
        
        var suggestion = await _dateSuggestionRepository.GetByIdAsync(suggestionId, cancellationToken);
        if (suggestion == null)
        {
            return AgentResult.CreateError($"Date suggestion not found with ID: {suggestionId}");
        }
        
        try
        {
            suggestion.Complete(rating);
            
            var updated = await _dateSuggestionRepository.UpdateAsync(suggestion, cancellationToken);
            
            _logger.LogInformation("Date suggestion {SuggestionId} completed with rating {Rating}", suggestionId, rating);
            
            return BuildSuggestionResult(updated);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            return AgentResult.CreateError(ex.Message);
        }
    }
    
    private async Task<AgentResult> GetSuggestionsAsync(
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken)
    {
        if (!parameters.TryGetValue("matchId", out var matchIdObj) || !Guid.TryParse(matchIdObj.ToString(), out var matchId))
        {
            return AgentResult.CreateError("Missing or invalid required parameter: matchId");
        }
        
        var suggestions = await _dateSuggestionRepository.GetByMatchIdAsync(matchId, cancellationToken);
        
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["suggestions"] = suggestions.Select(s => new Dictionary<string, object>
            {
                ["id"] = s.Id,
                ["title"] = s.Title,
                ["description"] = s.Description,
                ["location"] = s.Location,
                ["category"] = s.Category,
                ["estimatedCost"] = s.EstimatedCost,
                ["status"] = s.GetStatus(),
                ["isAccepted"] = s.IsAccepted,
                ["isRejected"] = s.IsRejected,
                ["scheduledDate"] = s.ScheduledDate?.ToString("O") ?? (object)DBNull.Value,
                ["rating"] = s.Rating.HasValue ? s.Rating.Value : (object)DBNull.Value
            }).ToList()
        });
    }
    
    private static AgentResult BuildSuggestionResult(DateSuggestion suggestion)
    {
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["id"] = suggestion.Id,
            ["title"] = suggestion.Title,
            ["status"] = suggestion.GetStatus(),
            ["isAccepted"] = suggestion.IsAccepted,
            ["isRejected"] = suggestion.IsRejected,
            ["scheduledDate"] = suggestion.ScheduledDate?.ToString("O") ?? (object)DBNull.Value,
            ["isCompleted"] = suggestion.IsCompleted,
            ["rating"] = suggestion.Rating.HasValue ? suggestion.Rating.Value : (object)DBNull.Value
        });
    }
}
