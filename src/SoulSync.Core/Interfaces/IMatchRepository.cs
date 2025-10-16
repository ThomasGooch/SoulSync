using SoulSync.Core.Domain;

namespace SoulSync.Core.Interfaces;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Match> CreateAsync(Match match, CancellationToken cancellationToken = default);
    Task<Match> UpdateAsync(Match match, CancellationToken cancellationToken = default);
    Task<IEnumerable<Match>> GetMatchesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Match>> GetPendingMatchesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MatchExistsAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);
}
