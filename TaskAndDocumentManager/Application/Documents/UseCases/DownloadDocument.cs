using System.IO;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class DownloadDocument
{
    private readonly IDocumentRepository _documentRepository;
    private readonly DocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IFileStorageService _fileStorageService;

    public DownloadDocument(
        IDocumentRepository documentRepository,
        DocumentAccessEvaluator documentAccessEvaluator,
        IFileStorageService fileStorageService)
    {
        _documentRepository = documentRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _fileStorageService = fileStorageService;
    }

    public async Task<Stream> ExecuteAsync(
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

        return await _fileStorageService.OpenReadAsync(document.StoragePath, cancellationToken);
    }
}
