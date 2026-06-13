using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class GetSharedDocuments
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;

    public GetSharedDocuments(
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository)
    {
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
    }

    public async Task<PaginatedResult<DocumentMetadataDto>> ExecuteAsync(
        Guid requestedByUserId,
        DocumentSearchQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requested by user ID is required.", nameof(requestedByUserId));
        }

        var sharedDocumentIds = await _documentAccessRepository.GetSharedDocumentIdsForUserAsync(
            requestedByUserId,
            cancellationToken);

        var normalizedQuery = NormalizeQuery(query);

        if (sharedDocumentIds.Count == 0)
        {
            return new PaginatedResult<DocumentMetadataDto>(
                Array.Empty<DocumentMetadataDto>(),
                0,
                normalizedQuery.PageNumber,
                normalizedQuery.PageSize);
        }

        var sharedDocumentIdSet = sharedDocumentIds.ToHashSet();
        var documents = await _documentRepository.GetAllAsync(cancellationToken);

        var sharedDocuments = ApplySearch(documents, sharedDocumentIdSet, normalizedQuery)
            .OrderByDescending(document => document.UploadedAtUtc)
            .Select(ToDto)
            .ToList();

        var items = sharedDocuments
            .Skip((normalizedQuery.PageNumber - 1) * normalizedQuery.PageSize)
            .Take(normalizedQuery.PageSize)
            .ToList();

        return new PaginatedResult<DocumentMetadataDto>(
            items,
            sharedDocuments.Count,
            normalizedQuery.PageNumber,
            normalizedQuery.PageSize);
    }

    private static DocumentSearchQuery NormalizeQuery(DocumentSearchQuery? query)
    {
        query ??= DocumentSearchQuery.Empty;

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1
            ? DocumentSearchQuery.DefaultPageSize
            : Math.Min(query.PageSize, DocumentSearchQuery.MaxPageSize);

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
        HashSet<Guid> sharedDocumentIds,
        DocumentSearchQuery query)
    {
        var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
            ? null
            : query.SearchTerm.Trim();
        var contentType = string.IsNullOrWhiteSpace(query.ContentType)
            ? null
            : query.ContentType.Trim();

        var filteredDocuments = documents
            .Where(document => sharedDocumentIds.Contains(document.Id))
            .AsEnumerable();

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

        return filteredDocuments;
    }

    private static DocumentMetadataDto ToDto(Document document)
    {
        return new DocumentMetadataDto(
            document.Id,
            document.OriginalFileName,
            document.ContentType,
            document.SizeInBytes,
            document.OwnerId,
            document.UploadedAtUtc,
            document.LinkedTaskId);
    }
}
