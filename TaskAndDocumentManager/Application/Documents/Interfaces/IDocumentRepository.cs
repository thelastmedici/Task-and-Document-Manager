using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Document>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
