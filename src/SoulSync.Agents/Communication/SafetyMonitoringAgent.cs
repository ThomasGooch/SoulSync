using Microsoft.Extensions.Logging;
using SoulSync.Core.Agents;
using SoulSync.Core.Domain;
using SoulSync.Core.Enums;
using SoulSync.Core.Interfaces;

namespace SoulSync.Agents.Communication;

public class SafetyMonitoringAgent : BaseAgent
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISafetyFlagRepository _safetyFlagRepository;
    private readonly IAIService _aiService;
    private readonly ILogger<SafetyMonitoringAgent> _logger;

    public SafetyMonitoringAgent(
        IMessageRepository messageRepository,
        ISafetyFlagRepository safetyFlagRepository,
        IAIService aiService,
        ILogger<SafetyMonitoringAgent> logger)
    {
        _messageRepository = messageRepository;
        _safetyFlagRepository = safetyFlagRepository;
        _aiService = aiService;
        _logger = logger;
    }

    protected override async Task<AgentResult> ExecuteInternalAsync(
        AgentRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Validate parameters
        if (!request.Parameters.TryGetValue("messageId", out var messageIdObj) ||
            !Guid.TryParse(messageIdObj?.ToString(), out var messageId))
        {
            return AgentResult.CreateError("Missing or invalid messageId parameter");
        }

        // 2. Get message
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        if (message == null)
        {
            return AgentResult.CreateError($"Message not found: {messageId}");
        }

        // 3. Analyze safety with AI
        SafetyLevel safetyLevel;
        bool usedFallback = false;
        List<string> issues = new();

        try
        {
            var analysis = await _aiService.AnalyzeSafetyAsync(message.Content, cancellationToken);
            
            var safetyLevelStr = analysis["safetyLevel"]?.ToString() ?? "Safe";
            safetyLevel = ParseSafetyLevel(safetyLevelStr);
            
            if (analysis.TryGetValue("issues", out var issuesObj) && issuesObj is List<string> issuesList)
            {
                issues = issuesList;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service unavailable, using fallback safety analysis");
            safetyLevel = FallbackSafetyAnalysis(message.Content);
            usedFallback = true;
        }

        // 4. Take action based on safety level
        bool requiresAction = safetyLevel >= SafetyLevel.Warning;

        if (requiresAction)
        {
            // Create safety flag
            var safetyFlag = new SafetyFlag
            {
                MessageId = messageId,
                ConversationId = message.ConversationId,
                FlaggedByUserId = message.ReceiverId, // System flagged
                Reason = issues.Any() ? string.Join(", ", issues) : "Content flagged by safety monitoring",
                Level = safetyLevel
            };

            await _safetyFlagRepository.CreateAsync(safetyFlag, cancellationToken);

            // Flag message if dangerous
            if (safetyLevel >= SafetyLevel.Dangerous)
            {
                message.Flag("Flagged by safety monitoring system");
                await _messageRepository.UpdateAsync(message, cancellationToken);
            }

            _logger.LogWarning("Message {MessageId} flagged with safety level {SafetyLevel}",
                messageId, safetyLevel);
        }

        // 5. Return result
        return AgentResult.CreateSuccess(new Dictionary<string, object>
        {
            ["messageId"] = messageId,
            ["safetyLevel"] = safetyLevel,
            ["requiresAction"] = requiresAction,
            ["issues"] = issues,
            ["usedFallback"] = usedFallback
        });
    }

    private SafetyLevel ParseSafetyLevel(string level)
    {
        return level.ToLower() switch
        {
            "safe" => SafetyLevel.Safe,
            "suspicious" => SafetyLevel.Suspicious,
            "warning" => SafetyLevel.Warning,
            "dangerous" => SafetyLevel.Dangerous,
            "blocked" => SafetyLevel.Blocked,
            _ => SafetyLevel.Suspicious
        };
    }

    private SafetyLevel FallbackSafetyAnalysis(string content)
    {
        // Simple keyword-based fallback
        var lowerContent = content.ToLower();
        
        var dangerousKeywords = new[] { "threat", "harm", "violence", "kill" };
        var warningKeywords = new[] { "hate", "abuse", "harass", "offensive" };
        var suspiciousKeywords = new[] { "spam", "scam", "money", "click here" };

        if (dangerousKeywords.Any(k => lowerContent.Contains(k)))
            return SafetyLevel.Dangerous;
        
        if (warningKeywords.Any(k => lowerContent.Contains(k)))
            return SafetyLevel.Warning;
        
        if (suspiciousKeywords.Any(k => lowerContent.Contains(k)))
            return SafetyLevel.Suspicious;

        return SafetyLevel.Safe;
    }
}
