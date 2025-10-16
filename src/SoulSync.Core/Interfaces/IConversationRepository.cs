using SoulSync.Core.Domain;

namespace SoulSync.Core.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Conversation> CreateAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<Conversation> UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<IEnumerable<Conversation>> GetConversationsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationForMatchAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<bool> ConversationExistsAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
}
