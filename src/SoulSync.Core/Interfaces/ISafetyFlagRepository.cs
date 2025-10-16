using SoulSync.Core.Domain;
using SoulSync.Core.Enums;

namespace SoulSync.Core.Interfaces;

public interface ISafetyFlagRepository
{
    Task<SafetyFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SafetyFlag> CreateAsync(SafetyFlag flag, CancellationToken cancellationToken = default);
    Task<SafetyFlag> UpdateAsync(SafetyFlag flag, CancellationToken cancellationToken = default);
    Task<IEnumerable<SafetyFlag>> GetUnresolvedFlagsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SafetyFlag>> GetFlagsByLevelAsync(SafetyLevel level, CancellationToken cancellationToken = default);
    Task<IEnumerable<SafetyFlag>> GetFlagsByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
