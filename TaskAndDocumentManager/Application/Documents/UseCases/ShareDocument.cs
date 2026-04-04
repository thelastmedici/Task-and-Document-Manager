using System.IO;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Documents;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class ShareDocument
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;

    public ShareDocument(
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository)
    {
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
    }

    public async Task ExecuteAsync(
        ShareDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (document.UploadedByUserId != request.GrantedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can share this document.");
        }

        var access = new DocumentAccess(request.DocumentId, request.TargetUserId, request.GrantedByUserId);
        await _documentAccessRepository.GrantAccessAsync(access, cancellationToken);
    }
}
