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
}