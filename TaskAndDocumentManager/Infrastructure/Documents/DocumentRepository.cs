using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

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

    public Task<IReadOnlyCollection<Document>> SearchDocumentsAsync(
        DocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var documents = ApplySearch(Documents, normalizedQuery)
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToList();

        return Task.FromResult((IReadOnlyCollection<Document>)documents);
    }

    public Task<PaginatedResult<Document>> SearchDocumentsPageAsync(
        DocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var matchingDocuments = ApplySearch(Documents, normalizedQuery)
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToList();

        var items = matchingDocuments
            .Skip((normalizedQuery.PageNumber - 1) * normalizedQuery.PageSize)
            .Take(normalizedQuery.PageSize)
            .ToList();

        return Task.FromResult(new PaginatedResult<Document>(
            items,
            matchingDocuments.Count,
            normalizedQuery.PageNumber,
            normalizedQuery.PageSize));
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

    private static DocumentQuery NormalizeQuery(DocumentQuery? query)
    {
        query ??= DocumentQuery.Empty;

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1
            ? DocumentQuery.DefaultPageSize
            : Math.Min(query.PageSize, DocumentQuery.MaxPageSize);

        if (query.UploadedFromUtc.HasValue && query.UploadedToUtc.HasValue &&
            query.UploadedFromUtc.Value > query.UploadedToUtc.Value)
        {
            throw new ArgumentException("Uploaded from date cannot be later than uploaded to date.", nameof(query));
        }

        return query with
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static IEnumerable<Document> ApplySearch(
        IEnumerable<Document> documents,
        DocumentQuery query)
    {
        var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
            ? null
            : query.SearchTerm.Trim();
        var contentType = string.IsNullOrWhiteSpace(query.ContentType)
            ? null
            : query.ContentType.Trim();

        var filteredDocuments = documents;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredDocuments = filteredDocuments.Where(document =>
                document.OriginalFileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            filteredDocuments = filteredDocuments.Where(document =>
                string.Equals(document.ContentType, contentType, StringComparison.OrdinalIgnoreCase));
        }

        if (query.UploadedFromUtc.HasValue)
        {
            filteredDocuments = filteredDocuments.Where(document =>
                document.UploadedAtUtc >= query.UploadedFromUtc.Value);
        }

        if (query.UploadedToUtc.HasValue)
        {
            filteredDocuments = filteredDocuments.Where(document =>
                document.UploadedAtUtc <= query.UploadedToUtc.Value);
        }

        if (query.WorkspaceId.HasValue)
        {
            filteredDocuments = filteredDocuments.Where(document =>
                document.WorkspaceId == query.WorkspaceId.Value);
        }

        return filteredDocuments;
    }
}
