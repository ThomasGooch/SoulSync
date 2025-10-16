using SoulSync.Core.Domain;

namespace SoulSync.Core.Interfaces;

/// <summary>
/// Repository interface for managing date suggestion data persistence
/// </summary>
public interface IDateSuggestionRepository
{
    /// <summary>
    /// Gets a date suggestion by its ID
    /// </summary>
    Task<DateSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all date suggestions for a specific match
    /// </summary>
    Task<IEnumerable<DateSuggestion>> GetByMatchIdAsync(Guid matchId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new date suggestion
    /// </summary>
    Task<DateSuggestion> CreateAsync(DateSuggestion suggestion, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing date suggestion
    /// </summary>
    Task<DateSuggestion> UpdateAsync(DateSuggestion suggestion, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all pending date suggestions for a match
    /// </summary>
    Task<IEnumerable<DateSuggestion>> GetPendingSuggestionsAsync(Guid matchId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all accepted date suggestions for a match
    /// </summary>
    Task<IEnumerable<DateSuggestion>> GetAcceptedSuggestionsAsync(Guid matchId, CancellationToken cancellationToken = default);
}
