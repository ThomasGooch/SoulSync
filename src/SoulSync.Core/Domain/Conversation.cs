using SoulSync.Core.Enums;

namespace SoulSync.Core.Domain;

public class Conversation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid User1Id { get; set; }
    public Guid User2Id { get; set; }
    public Guid MatchId { get; set; }
    
    public ConversationType Type { get; set; } = ConversationType.Initial;
    public bool IsActive { get; set; } = true;
    public int MessageCount { get; private set; }
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; private set; }

    // Navigation properties
    public User? User1 { get; set; }
    public User? User2 { get; set; }
    public Match? Match { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public void AddMessage()
    {
        MessageCount++;
        LastMessageAt = DateTime.UtcNow;
    }

    public void UpdateType(ConversationType type)
    {
        Type = type;
    }

    public void Archive()
    {
        IsActive = false;
    }

    public bool IsUserInConversation(Guid userId)
    {
        return User1Id == userId || User2Id == userId;
    }

    public Guid GetOtherUserId(Guid userId)
    {
        if (!IsUserInConversation(userId))
            throw new ArgumentException("User is not part of this conversation", nameof(userId));

        return userId == User1Id ? User2Id : User1Id;
    }
}
