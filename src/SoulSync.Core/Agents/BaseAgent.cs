namespace SoulSync.Core.Agents;

public abstract class BaseAgent
{
    public async Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteInternalAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return AgentResult.CreateError($"Agent execution failed: {ex.Message}");
        }
    }

    protected abstract Task<AgentResult> ExecuteInternalAsync(AgentRequest request, CancellationToken cancellationToken);
}

public class AgentRequest
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

public class AgentResult
{
    public bool IsSuccess { get; set; }
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static AgentResult CreateSuccess(object? data = null)
    {
        return new AgentResult
        {
            IsSuccess = true,
            Data = data
        };
    }

    public static AgentResult CreateError(string errorMessage)
    {
        return new AgentResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}