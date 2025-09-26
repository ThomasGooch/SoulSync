namespace SoulSync.Core.Interfaces;

public interface IAIService
{
    Task<string> ProcessRequestAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> AnalyzeProfileAsync(string profileText, CancellationToken cancellationToken = default);
    Task<int> CalculateCompatibilityScoreAsync(string profile1, string profile2, CancellationToken cancellationToken = default);
}

public class AIServiceResponse
{
    public string Content { get; set; } = string.Empty;
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}