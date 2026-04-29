using System.IO;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Documents;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class UploadDocument
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;

    public UploadDocument(
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Guid> ExecuteAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Uploaded by user ID is required.", nameof(request.UploadedByUserId));
        }

        await using var contentStream = new MemoryStream(request.Content, writable: false);
        var storagePath = await _fileStorageService.SaveAsync(
            request.UploadedByUserId,
            request.FileName,
            contentStream,
            cancellationToken);
    
        var document = new Document(
            request.FileName,
            request.ContentType,
            request.Content.LongLength,
            storagePath,
            request.UploadedByUserId);
        

        await _documentRepository.AddAsync(document, cancellationToken);
        return document.Id;
    }
}
