namespace SoulSync.Core.Domain;

public enum MatchStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired
}

public class Match
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId1 { get; set; }
    public Guid UserId2 { get; set; }
    public int CompatibilityScore { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Pending;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }

    // Navigation properties
    public User? User1 { get; set; }
    public User? User2 { get; set; }
    public CompatibilityScore? DetailedScore { get; set; }

    public void Accept()
    {
        Status = MatchStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = MatchStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateCompatibilityScore(int newScore)
    {
        if (newScore < 0 || newScore > 100)
            throw new ArgumentException("Compatibility score must be between 0 and 100", nameof(newScore));

        CompatibilityScore = newScore;
        LastModifiedAt = DateTime.UtcNow;
    }

    public bool IsUserInMatch(Guid userId)
    {
        return UserId1 == userId || UserId2 == userId;
    }

    public Guid GetOtherUserId(Guid userId)
    {
        if (!IsUserInMatch(userId))
            throw new ArgumentException("User is not part of this match", nameof(userId));

        return userId == UserId1 ? UserId2 : UserId1;
    }
}
