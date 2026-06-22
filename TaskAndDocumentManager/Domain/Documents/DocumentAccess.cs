namespace TaskAndDocumentManager.Domain.Documents;

public class DocumentAccess
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DocumentId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public DateTime GrantedAtUtc { get; private set; } = DateTime.UtcNow;

    protected DocumentAccess()
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

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        DocumentId = documentId;
        UserId = userId;
        GrantedByUserId = grantedByUserId;
        WorkspaceId = workspaceId;
    }
}
