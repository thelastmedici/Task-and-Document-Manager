using TaskAndDocumentManager.Application.Documents.Interfaces;

using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class RevokeDocumentAccess
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;

    public RevokeDocumentAccess(
        IAuditLogRepository auditLogRepository,
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository)
    {
        _auditLogRepository = auditLogRepository;
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
    }

    public async Task ExecuteAsync(
        Guid documentId,
        Guid targetUserId,
        Guid revokedByUserId,
        Guid workspaceId,
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

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        var document = await _documentRepository.GetByIdInWorkspaceAsync(documentId, workspaceId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (!isAdmin && document.OwnerId != revokedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can revoke document access.");
        }

        if (targetUserId == document.OwnerId)
        {
            throw new InvalidOperationException("Owner access cannot be revoked.");
        }

        await _documentAccessRepository.RevokeAccessAsync(
            documentId,
            targetUserId,
            workspaceId,
            cancellationToken);
        await _auditLogRepository.AddAsync(
            new AuditLog(
                revokedByUserId,
                AuditActions.DocumentAccessRevoked,
                nameof(Document),
                documentId,
                workspaceId),
            cancellationToken);
    }
}
