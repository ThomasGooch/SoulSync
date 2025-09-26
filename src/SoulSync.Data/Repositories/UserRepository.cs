using Microsoft.EntityFrameworkCore;
using SoulSync.Core.Domain;
using SoulSync.Core.Interfaces;
using SoulSync.Data.Context;

namespace SoulSync.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SoulSyncDbContext _context;

    public UserRepository(SoulSyncDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(id, cancellationToken);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetPotentialMatchesAsync(Guid userId, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetByIdAsync(userId, cancellationToken);
        if (currentUser?.Profile == null)
            return Enumerable.Empty<User>();

        var currentProfile = currentUser.Profile;

        // Basic matching logic - exclude self and get users with compatible preferences
        var potentialMatches = await _context.Users
            .Include(u => u.Profile)
            .Where(u => u.Id != userId && 
                       u.IsActive && 
                       u.Profile != null &&
                       currentProfile.InterestedInGenders.Contains(u.Profile.GenderIdentity) &&
                       u.Profile.InterestedInGenders.Contains(currentProfile.GenderIdentity))
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        return potentialMatches;
    }
}