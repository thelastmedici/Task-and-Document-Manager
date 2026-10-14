namespace TaskAndDocumentManager.Domain.Documents;

public class DocumentAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public Guid GrantedByUserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;

    protected DocumentAccess()
    {
    }

    public DocumentAccess(Guid documentId, Guid userId, Guid grantedByUserId)
        : this(documentId, userId, grantedByUserId, Guid.Empty)
    {
    }

    public DocumentAccess(Guid documentId, Guid userId, Guid grantedByUserId, Guid workspaceId)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document ID is required.", nameof(documentId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (grantedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Granted by user ID is required.", nameof(grantedByUserId));
        }

        if (workspaceId != Guid.Empty)
        {
            WorkspaceId = workspaceId;
        }

        DocumentId = documentId;
        UserId = userId;
        GrantedByUserId = grantedByUserId;
        var now = DateTime.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        GrantedAtUtc = now;
    }

    public void UpdateTimestamp()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
