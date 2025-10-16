using SoulSync.Core.Enums;

namespace SoulSync.Core.Domain;

public class SafetyFlag
{
    private string _reason = string.Empty;

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid FlaggedByUserId { get; set; }
    
    public string Reason
    {
        get => _reason;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Reason cannot be empty", nameof(Reason));

            _reason = value;
        }
    }

    public SafetyLevel Level { get; set; } = SafetyLevel.Suspicious;
    public bool IsResolved { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; private set; }
    public string? ActionTaken { get; private set; }

    // Navigation properties
    public Message? Message { get; set; }
    public Conversation? Conversation { get; set; }
    public User? FlaggedByUser { get; set; }

    public void UpdateLevel(SafetyLevel level)
    {
        Level = level;
    }

    public void Resolve(string? actionTaken = null)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ActionTaken = actionTaken;
    }

    public void Escalate()
    {
        Level = Level switch
        {
            SafetyLevel.Safe => SafetyLevel.Suspicious,
            SafetyLevel.Suspicious => SafetyLevel.Warning,
            SafetyLevel.Warning => SafetyLevel.Dangerous,
            SafetyLevel.Dangerous => SafetyLevel.Blocked,
            SafetyLevel.Blocked => SafetyLevel.Blocked,
            _ => SafetyLevel.Suspicious
        };
    }
}
