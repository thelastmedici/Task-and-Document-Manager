using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Documents;

namespace TaskAndDocumentManager.Infrastructure.Documents;

public class DocumentAccessRepository : IDocumentAccessRepository
{
    private static readonly List<DocumentAccess> DocumentAccessEntries = new();

    public Task GrantAccessAsync(DocumentAccess documentAccess, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(documentAccess);

        var alreadyGranted = DocumentAccessEntries.Any(entry =>
            entry.DocumentId == documentAccess.DocumentId &&
            entry.UserId == documentAccess.UserId);

        if (!alreadyGranted)
        {
            DocumentAccessEntries.Add(documentAccess);
        }

        return Task.CompletedTask;
    }

    public Task<bool> HasAccessAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var hasAccess = DocumentAccessEntries.Any(entry =>
            entry.DocumentId == documentId &&
            entry.UserId == userId);

        return Task.FromResult(hasAccess);
    }

    public Task RevokeAccessAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var accessEntries = DocumentAccessEntries
            .Where(entry => entry.DocumentId == documentId && entry.UserId == userId)
            .ToList();

        foreach (var entry in accessEntries)
        {
            DocumentAccessEntries.Remove(entry);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Guid>> GetSharedDocumentIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var documentIds = DocumentAccessEntries
            .Where(entry => entry.UserId == userId)
            .Select(entry => entry.DocumentId)
            .Distinct()
            .ToList();

        return Task.FromResult((IReadOnlyCollection<Guid>)documentIds);
    }
}
