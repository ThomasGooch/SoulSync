using SoulSync.Core.Interfaces;

namespace SoulSync.Services.AI;

public class MockAIService : IAIService
{
    public async Task<string> ProcessRequestAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Simulate AI processing delay
        await Task.Delay(100, cancellationToken);
        
        // Return mock AI response based on prompt content
        if (prompt.Contains("profile") || prompt.Contains("bio"))
        {
            return "This profile shows outdoor enthusiasm, creativity, and social engagement. Personality traits suggest extroversion and openness to new experiences.";
        }
        
        return "AI analysis completed successfully.";
    }

    public async Task<string> AnalyzeProfileAsync(string profileText, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        
        var insights = new List<string>();
        
        if (profileText.Contains("hiking") || profileText.Contains("outdoor"))
            insights.Add("Strong outdoor enthusiasm");
            
        if (profileText.Contains("cooking") || profileText.Contains("food"))
            insights.Add("Culinary passion");
            
        if (profileText.Contains("travel") || profileText.Contains("adventure"))
            insights.Add("Adventure-seeking personality");
            
        if (profileText.Contains("reading") || profileText.Contains("books"))
            insights.Add("Intellectual curiosity");

        return insights.Any() 
            ? $"Profile analysis: {string.Join(", ", insights)}. Suggests an engaging and well-rounded personality."
            : "Profile shows diverse interests and a positive outlook on life.";
    }

    public async Task<int> CalculateCompatibilityScoreAsync(string profile1, string profile2, CancellationToken cancellationToken = default)
    {
        await Task.Delay(75, cancellationToken);
        
        // Simple mock calculation based on shared keywords
        var profile1Words = profile1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var profile2Words = profile2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var commonWords = profile1Words.Intersect(profile2Words).Count();
        var totalWords = profile1Words.Union(profile2Words).Count();
        
        if (totalWords == 0) return 50;
        
        var compatibilityScore = Math.Min(100, (int)((double)commonWords / totalWords * 100) + 30);
        
        return Math.Max(10, compatibilityScore); // Minimum 10% compatibility
    }

    public async Task<Dictionary<string, object>> AnalyzeSafetyAsync(string content, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        
        var lowerContent = content.ToLowerInvariant();
        var issues = new List<string>();
        var safetyLevel = "Safe";
        var score = 95;

        // Check for dangerous keywords
        var dangerousKeywords = new[] { "threat", "harm", "violence", "kill" };
        if (dangerousKeywords.Any(k => lowerContent.Contains(k)))
        {
            safetyLevel = "Dangerous";
            score = 10;
            issues.Add("Threats or violence detected");
        }
        else
        {
            // Check for warning keywords
            var warningKeywords = new[] { "hate", "abuse", "harass", "offensive" };
            if (warningKeywords.Any(k => lowerContent.Contains(k)))
            {
                safetyLevel = "Warning";
                score = 40;
                issues.Add("Inappropriate language");
            }
            else
            {
                // Check for suspicious keywords
                var suspiciousKeywords = new[] { "spam", "scam", "money", "click here" };
                if (suspiciousKeywords.Any(k => lowerContent.Contains(k)))
                {
                    safetyLevel = "Suspicious";
                    score = 60;
                    issues.Add("Suspicious content");
                }
            }
        }

        return new Dictionary<string, object>
        {
            ["safetyLevel"] = safetyLevel,
            ["score"] = score,
            ["issues"] = issues
        };
    }

    public async Task<Dictionary<string, object>> AnalyzeConversationAsync(string conversationHistory, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        
        var messageCount = conversationHistory.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        var engagement = messageCount > 10 ? "High" : messageCount > 5 ? "Medium" : "Low";
        
        var sentiment = "Positive";
        var lowerHistory = conversationHistory.ToLowerInvariant();
        
        // Simple sentiment analysis
        var positiveWords = new[] { "great", "love", "wonderful", "amazing", "excited", "happy" };
        var negativeWords = new[] { "bad", "hate", "terrible", "awful", "boring", "sad" };
        
        var positiveCount = positiveWords.Count(w => lowerHistory.Contains(w));
        var negativeCount = negativeWords.Count(w => lowerHistory.Contains(w));
        
        if (negativeCount > positiveCount)
            sentiment = "Negative";
        else if (positiveCount == 0 && negativeCount == 0)
            sentiment = "Neutral";

        var suggestions = new List<string>();
        
        if (engagement == "Low")
        {
            suggestions.Add("Try asking open-ended questions to encourage conversation");
            suggestions.Add("Show interest in their hobbies and passions");
        }
        else if (engagement == "High")
        {
            suggestions.Add("Consider suggesting a date or video call");
            suggestions.Add("Share more about your interests and values");
        }
        else
        {
            suggestions.Add("Keep the positive energy going");
            suggestions.Add("Ask deeper questions to build emotional connection");
        }

        return new Dictionary<string, object>
        {
            ["engagement"] = engagement,
            ["sentiment"] = sentiment,
            ["suggestions"] = suggestions
        };
    }
}