using SoulSync.Core.Domain;

namespace SoulSync.Core.Interfaces;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPreferences> CreateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<UserPreferences> UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<UserPreferences> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);
}
