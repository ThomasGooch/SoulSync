using SoulSync.Core.Domain;

namespace SoulSync.Core.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Message> CreateAsync(Message message, CancellationToken cancellationToken = default);
    Task<Message> UpdateAsync(Message message, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetMessagesByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetUnreadMessagesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetFlaggedMessagesAsync(CancellationToken cancellationToken = default);
}
