using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class ListAccessibleDocuments
{
    private readonly IDocumentRepository _documentRepository;
    private readonly DocumentAccessEvaluator _documentAccessEvaluator;

    public ListAccessibleDocuments(
        IDocumentRepository documentRepository,
        DocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<PaginatedResult<DocumentMetadataDto>> ExecuteAsync(
        Guid requestedByUserId,
        bool allowTaskParticipationAccess = false,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            requestedByUserId,
            allowTaskParticipationAccess,
            DocumentSearchQuery.Empty,
            cancellationToken);
    }

    public async Task<PaginatedResult<DocumentMetadataDto>> ExecuteAsync(
        Guid requestedByUserId,
        bool allowTaskParticipationAccess,
        DocumentSearchQuery? query,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requested by user ID is required.", nameof(requestedByUserId));
        }

        var normalizedQuery = NormalizeQuery(query);
        var documents = await _documentRepository.GetAllAsync(cancellationToken);
        var accessibleDocuments = new List<DocumentMetadataDto>();

        foreach (var document in ApplySearch(documents, normalizedQuery).OrderByDescending(document => document.UploadedAtUtc))
        {
            if (!await _documentAccessEvaluator.HasAccessAsync(
                document,
                requestedByUserId,
                allowTaskParticipationAccess,
                cancellationToken))
            {
                continue;
            }

            accessibleDocuments.Add(new DocumentMetadataDto(
                document.Id,
                document.OriginalFileName,
                document.ContentType,
                document.SizeInBytes,
                document.OwnerId,
                document.UploadedAtUtc,
                document.LinkedTaskId));
        }

        return ToPaginatedResult(accessibleDocuments, normalizedQuery);
    }

    public async Task<PaginatedResult<DocumentMetadataDto>> ExecuteForAdminAsync(
        DocumentSearchQuery? query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var documents = await _documentRepository.GetAllAsync(cancellationToken);

        var items = ApplySearch(documents, normalizedQuery)
            .OrderByDescending(document => document.UploadedAtUtc)
            .Select(ToDto)
            .ToList();

        return ToPaginatedResult(items, normalizedQuery);
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
        DocumentSearchQuery query)
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

        return filteredDocuments;
    }

    private static PaginatedResult<DocumentMetadataDto> ToPaginatedResult(
        IReadOnlyList<DocumentMetadataDto> documents,
        DocumentSearchQuery query)
    {
        var items = documents
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PaginatedResult<DocumentMetadataDto>(
            items,
            documents.Count,
            query.PageNumber,
            query.PageSize);
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
