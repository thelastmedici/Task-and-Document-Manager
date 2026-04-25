using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class GetDocumentMetadata
{
    private readonly IDocumentRepository _documentRepository;
    private readonly DocumentAccessEvaluator _documentAccessEvaluator;

    public GetDocumentMetadata(
        IDocumentRepository documentRepository,
        DocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<DocumentMetadataDto> ExecuteAsync(
        Guid documentId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        var hasAccess = await _documentAccessEvaluator.HasAccessAsync(
            document,
            requestedByUserId,
            cancellationToken);

        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("You do not have access to this document.");
        }

        return new DocumentMetadataDto(
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeInBytes,
            document.UploadedByUserId,
            document.UploadedAtUtc,
            document.LinkedTaskId);
    }
}
