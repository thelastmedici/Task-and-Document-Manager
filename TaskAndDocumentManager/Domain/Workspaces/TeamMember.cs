namespace TaskAndDocumentManager.Domain.Workspaces;

public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeamId { get; set; }

    public Guid UserId { get; set; }

    public Team? Team { get; set; }

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    protected TeamMember()
    {
    }

    public TeamMember(Guid teamId, Guid userId)
    {
        if (teamId == Guid.Empty)
        {
            throw new ArgumentException("Team ID is required.", nameof(teamId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        TeamId = teamId;
        UserId = userId;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        JoinedAtUtc = CreatedAtUtc;
    }

    public void UpdateTimestamp()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
