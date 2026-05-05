using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;

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

    public async Task<IReadOnlyList<DocumentMetadataDto>> ExecuteAsync(
        Guid requestedByUserId,
        bool allowTaskParticipationAccess = false,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requested by user ID is required.", nameof(requestedByUserId));
        }

        var documents = await _documentRepository.GetAllAsync(cancellationToken);
        var accessibleDocuments = new List<DocumentMetadataDto>();

        foreach (var document in documents.OrderByDescending(document => document.UploadedAtUtc))
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

        return accessibleDocuments;
    }
}
