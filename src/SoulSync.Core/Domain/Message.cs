using SoulSync.Core.Enums;

namespace SoulSync.Core.Domain;

public class Message
{
    private string _content = string.Empty;

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public Guid ConversationId { get; set; }

    public string Content
    {
        get => _content;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Content cannot be empty", nameof(Content));

            _content = value;
        }
    }

    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    
    public bool IsFlagged { get; private set; }
    public string? FlaggedReason { get; private set; }
    public DateTime? FlaggedAt { get; private set; }

    // Navigation properties
    public User? Sender { get; set; }
    public User? Receiver { get; set; }
    public Conversation? Conversation { get; set; }

    public void MarkAsDelivered()
    {
        Status = MessageStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    public void MarkAsRead()
    {
        Status = MessageStatus.Read;
        ReadAt = DateTime.UtcNow;
    }

    public void Flag(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Flag reason cannot be empty", nameof(reason));

        IsFlagged = true;
        FlaggedReason = reason;
        FlaggedAt = DateTime.UtcNow;
        Status = MessageStatus.Flagged;
    }

    public bool IsFromUser(Guid userId)
    {
        return SenderId == userId;
    }
}
