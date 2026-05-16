using TaskAndDocumentManager.Domain.Documents;

namespace TaskAndDocumentManager.Application.Documents.Interfaces;

public interface IDocumentAccessRepository
{
    Task GrantAccessAsync(DocumentAccess documentAccess, CancellationToken cancellationToken = default);
    Task<bool> HasAccessAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAccessAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Guid>> GetSharedDocumentIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
