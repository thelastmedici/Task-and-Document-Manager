using System.IO;
using Microsoft.Extensions.Logging;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class DownloadDocument
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DownloadDocument> _logger;

    public DownloadDocument(
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        ILogger<DownloadDocument> logger)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<DownloadDocumentResult> ExecuteAsync(
        Guid documentId,
        Guid requestedByUserId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requested by user ID is required.", nameof(requestedByUserId));
        }

        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (!isAdmin && document.OwnerId != requestedByUserId)
        {
            throw new UnauthorizedAccessException("You do not have access to this document.");
        }

        try
        {
            var stream = await _fileStorageService.OpenReadAsync(document.StoragePath, cancellationToken);

            return new DownloadDocumentResult(
                stream,
                document.ContentType,
                document.OriginalFileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(
                ex,
                "Stored file missing for document {DocumentId}.",
                document.Id);

            throw new InvalidOperationException("Document could not be retrieved.", ex);
        }
    }
}
