using System.IO;
using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class DeleteDocument
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;

    public DeleteDocument(
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task ExecuteAsync(
        Guid documentId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (document.UploadedByUserId != requestedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can delete this document.");
        }

        await _fileStorageService.DeleteAsync(document.StoragePath, cancellationToken);
        await _documentRepository.DeleteAsync(documentId, cancellationToken);
    }
}
