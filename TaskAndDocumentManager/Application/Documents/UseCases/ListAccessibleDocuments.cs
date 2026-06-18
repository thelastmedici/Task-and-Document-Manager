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
            DocumentQuery.Empty,
            cancellationToken);
    }

    public async Task<PaginatedResult<DocumentMetadataDto>> ExecuteAsync(
        Guid requestedByUserId,
        Guid workspaceId,
        bool allowTaskParticipationAccess,
        DocumentQuery? query,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        return await ExecuteAsync(
            requestedByUserId,
            allowTaskParticipationAccess,
            (query ?? DocumentQuery.Empty) with { WorkspaceId = workspaceId },
            cancellationToken);
    }

    public async Task<PaginatedResult<DocumentMetadataDto>> ExecuteAsync(
        Guid requestedByUserId,
        bool allowTaskParticipationAccess,
        DocumentQuery? query,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requested by user ID is required.", nameof(requestedByUserId));
        }

        var normalizedQuery = NormalizeQuery(query);
        var documents = await _documentRepository.SearchDocumentsAsync(normalizedQuery, cancellationToken);
        var accessibleDocuments = new List<DocumentMetadataDto>();

        foreach (var document in documents)
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
        DocumentQuery? query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var documents = await _documentRepository.SearchDocumentsPageAsync(normalizedQuery, cancellationToken);

        var items = documents.Items
            .Select(ToDto)
            .ToList();

        return new PaginatedResult<DocumentMetadataDto>(
            items,
            documents.TotalCount,
            documents.Page,
            documents.PageSize);
    }

    public async Task<PaginatedResult<DocumentMetadataDto>> ExecuteForAdminAsync(
        Guid workspaceId,
        DocumentQuery? query,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        return await ExecuteForAdminAsync(
            (query ?? DocumentQuery.Empty) with { WorkspaceId = workspaceId },
            cancellationToken);
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

    private static PaginatedResult<DocumentMetadataDto> ToPaginatedResult(
        IReadOnlyList<DocumentMetadataDto> documents,
        DocumentQuery query)
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
