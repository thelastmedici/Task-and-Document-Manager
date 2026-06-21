namespace TaskAndDocumentManager.Domain.Workspaces;

public class TeamMember
{
    public Guid TeamId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAtUtc { get; private set; } = DateTime.UtcNow;

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
    }
}
