using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Documents;

namespace TaskAndDocumentManager.Infrastructure.Documents;

public class DocumentRepository : IDocumentRepository
{
    private static readonly List<Document> Documents = new();

    public Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        Documents.Add(document);
        return Task.CompletedTask;
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = Documents.FirstOrDefault(existingDocument => existingDocument.Id == id);
        return Task.FromResult(document);
    }

    public Task<IReadOnlyCollection<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((IReadOnlyCollection<Document>)Documents.ToList());
    }

    public Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var existingDocument = Documents.FirstOrDefault(item => item.Id == document.Id);

        if (existingDocument is not null)
        {
            Documents.Remove(existingDocument);
        }

        Documents.Add(document);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = Documents.FirstOrDefault(existingDocument => existingDocument.Id == id);

        if (document is not null)
        {
            Documents.Remove(document);
        }

        return Task.CompletedTask;
    }
}
