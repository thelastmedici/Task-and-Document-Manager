using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.Interfaces;

public static class DocumentRepositoryExtensions
{
    public static async Task<Document?> GetByIdInWorkspaceAsync(
        this IDocumentRepository documentRepository,
        Guid documentId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentRepository);

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        var document = await documentRepository.GetByIdAsync(documentId, cancellationToken);

        return document?.WorkspaceId == workspaceId ? document : null;
    }
}
