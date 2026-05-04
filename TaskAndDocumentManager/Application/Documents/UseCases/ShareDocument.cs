using System.IO;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Documents;
using TaskAndDocumentManager.Domain.Entities;

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

        if (request.TargetUserId == Guid.Empty)
        {
            throw new ArgumentException("Target user ID is required.", nameof(request.TargetUserId));
        }

        if (request.GrantedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Granted by user ID is required.", nameof(request.GrantedByUserId));
        }

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (document.UploadedByUserId != request.GrantedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can share this document.");
        }

        if (request.TargetUserId == request.GrantedByUserId)
        {
            throw new InvalidOperationException("You cannot share a document with yourself.");
        }

        var access = new DocumentAccess(request.DocumentId, request.TargetUserId, request.GrantedByUserId);
        await _documentAccessRepository.GrantAccessAsync(access, cancellationToken);
    }
}
