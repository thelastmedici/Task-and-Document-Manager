using System.IO;
using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class DownloadDocument
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;
    private readonly IFileStorageService _fileStorageService;

    public DownloadDocument(
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository,
        IFileStorageService fileStorageService)
    {
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Stream> ExecuteAsync(
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

        return await _fileStorageService.OpenReadAsync(document.StoragePath, cancellationToken);
    }
}
