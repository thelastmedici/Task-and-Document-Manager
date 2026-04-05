using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class GetDocumentMetadata
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;

    public GetDocumentMetadata(
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository)
    {
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
    }

    public async Task<DocumentMetadataDto> ExecuteAsync(
        Guid documentId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        var hasAccess = document.UploadedByUserId == requestedByUserId ||
            await _documentAccessRepository.HasAccessAsync(documentId, requestedByUserId, cancellationToken);

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
