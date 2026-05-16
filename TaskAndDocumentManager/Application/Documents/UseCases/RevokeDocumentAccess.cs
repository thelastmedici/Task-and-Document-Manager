using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class RevokeDocumentAccess
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;

    public RevokeDocumentAccess(
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository)
    {
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
    }

    public async Task ExecuteAsync(
        Guid documentId,
        Guid targetUserId,
        Guid revokedByUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (targetUserId == Guid.Empty)
        {
            throw new ArgumentException("Target user ID is required.", nameof(targetUserId));
        }

        if (revokedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Revoked by user ID is required.", nameof(revokedByUserId));
        }

        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (!isAdmin && document.OwnerId != revokedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can revoke document access.");
        }

        if (targetUserId == document.OwnerId)
        {
            throw new InvalidOperationException("Owner access cannot be revoked.");
        }

        await _documentAccessRepository.RevokeAccessAsync(documentId, targetUserId, cancellationToken);
    }
}
