using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Document>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Document>> SearchAsync(
        DocumentSearchQuery query,
        CancellationToken cancellationToken = default);
    Task<PaginatedResult<Document>> SearchPageAsync(
        DocumentSearchQuery query,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
