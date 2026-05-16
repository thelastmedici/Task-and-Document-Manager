using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;

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

    public async Task<IReadOnlyList<DocumentMetadataDto>> ExecuteAsync(
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requested by user ID is required.", nameof(requestedByUserId));
        }

        var sharedDocumentIds = await _documentAccessRepository.GetSharedDocumentIdsForUserAsync(
            requestedByUserId,
            cancellationToken);

        if (sharedDocumentIds.Count == 0)
        {
            return [];
        }

        var sharedDocumentIdSet = sharedDocumentIds.ToHashSet();
        var documents = await _documentRepository.GetAllAsync(cancellationToken);

        return documents
            .Where(document => sharedDocumentIdSet.Contains(document.Id))
            .OrderByDescending(document => document.UploadedAtUtc)
            .Select(document => new DocumentMetadataDto(
                document.Id,
                document.OriginalFileName,
                document.ContentType,
                document.SizeInBytes,
                document.OwnerId,
                document.UploadedAtUtc,
                document.LinkedTaskId))
            .ToList();
    }
}
