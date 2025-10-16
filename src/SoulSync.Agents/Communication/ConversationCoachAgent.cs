using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Communication;

public class ConversationCoachAgent : BaseAgent
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IAIService _aiService;
    private readonly ILogger<ConversationCoachAgent> _logger;

    public ConversationCoachAgent(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IAIService aiService,
        ILogger<ConversationCoachAgent> logger)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _aiService = aiService;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Validate parameters
        if (!request.Parameters.TryGetValue("conversationId", out var conversationIdObj) ||
            !Guid.TryParse(conversationIdObj?.ToString(), out var conversationId))
        {
            return AgentResult.CreateError("Missing or invalid conversationId parameter");
        }

        if (!request.Parameters.TryGetValue("userId", out var userIdObj) ||
            !Guid.TryParse(userIdObj?.ToString(), out var userId))
        {
            return AgentResult.CreateError("Missing or invalid userId parameter");
        }

        // 2. Get conversation
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            return AgentResult.CreateError($"Conversation not found: {conversationId}");
        }

        // 3. Get messages
        var messages = (await _messageRepository.GetMessagesByConversationIdAsync(conversationId, cancellationToken)).ToList();

        // 4. Handle no messages case
        if (!messages.Any())
        {
            return AgentResult.CreateSuccess(new Dictionary<string, object>
            {
                ["conversationHealth"] = "Initial",
                ["suggestions"] = GetInitialConversationTips(),
                ["sentiment"] = "Neutral"
            });
        }

        // 5. Analyze conversation with AI
        string conversationHealth;
        List<string> suggestions;
        string sentiment;
        bool usedFallback = false;

        try
        {
            var conversationHistory = BuildConversationHistory(messages, userId);
            var analysis = await _aiService.AnalyzeConversationAsync(conversationHistory, cancellationToken);

            var engagement = analysis["engagement"]?.ToString() ?? "Medium";
            sentiment = analysis["sentiment"]?.ToString() ?? "Neutral";
            
            conversationHealth = DetermineConversationHealth(engagement, sentiment, messages.Count);
            
            if (analysis.TryGetValue("suggestions", out var suggestionsObj) && suggestionsObj is List<string> aiSuggestions)
            {
                suggestions = aiSuggestions;
            }
            else
            {
                suggestions = GetDefaultSuggestions(engagement);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service unavailable, using fallback conversation analysis");
            conversationHealth = FallbackConversationHealth(messages);
            suggestions = GetFallbackSuggestions(messages.Count);
            sentiment = "Neutral";
            usedFallback = true;
        }

        _logger.LogInformation("Conversation {ConversationId} analyzed: Health={Health}, Suggestions={Count}",
            conversationId, conversationHealth, suggestions.Count);

        // 6. Return result
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["conversationHealth"] = conversationHealth,
            ["suggestions"] = suggestions,
            ["sentiment"] = sentiment,
            ["messageCount"] = messages.Count,
            ["usedFallback"] = usedFallback
        });
    }

    private string BuildConversationHistory(List<Message> messages, Guid userId)
    {
        var history = string.Join("\n", messages
            .OrderBy(m => m.CreatedAt)
            .Take(20) // Last 20 messages
            .Select(m => $"{(m.SenderId == userId ? "User" : "Partner")}: {m.Content}"));

        return history;
    }

    private string DetermineConversationHealth(string engagement, string sentiment, int messageCount)
    {
        if (engagement.Equals("High", StringComparison.OrdinalIgnoreCase) && 
            sentiment.Equals("Positive", StringComparison.OrdinalIgnoreCase))
            return "Excellent";

        if (engagement.Equals("Low", StringComparison.OrdinalIgnoreCase) || 
            messageCount < 5)
            return "NeedsAttention";

        if (sentiment.Equals("Negative", StringComparison.OrdinalIgnoreCase))
            return "NeedsAttention";

        return "Good";
    }

    private string FallbackConversationHealth(List<Message> messages)
    {
        if (messages.Count < 3)
            return "NeedsAttention";
        
        if (messages.Count > 10)
            return "Good";

        return "Initial";
    }

    private List<string> GetInitialConversationTips()
    {
        return new List<string>
        {
            "Start with a friendly greeting and reference their profile",
            "Ask an open-ended question about their interests",
            "Share something interesting about yourself",
            "Keep your first message light and positive"
        };
    }

    private List<string> GetDefaultSuggestions(string engagement)
    {
        if (engagement.Equals("Low", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                "Try asking open-ended questions to encourage conversation",
                "Show genuine interest in their hobbies and passions",
                "Share personal stories to build connection",
                "Suggest a specific activity you could do together"
            };
        }

        return new List<string>
        {
            "Keep the positive energy going",
            "Consider suggesting a date or video call",
            "Share more about your interests and values",
            "Ask deeper questions to build emotional connection"
        };
    }

    private List<string> GetFallbackSuggestions(int messageCount)
    {
        if (messageCount < 5)
        {
            return new List<string>
            {
                "Ask open-ended questions to learn more about them",
                "Share something personal to build trust",
                "Show interest in their experiences and stories"
            };
        }

        return new List<string>
        {
            "Consider moving to a phone or video call",
            "Suggest meeting in person if you feel comfortable",
            "Share your goals and what you're looking for",
            "Ask about their ideal date or relationship"
        };
    }
}
