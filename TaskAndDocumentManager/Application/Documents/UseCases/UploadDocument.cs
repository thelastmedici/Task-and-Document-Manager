using System.IO;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class UploadDocument
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".txt",
        ".png",
        ".jpg",
        ".jpeg"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

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

        if(request.UploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Uploaded by user ID is required.", nameof(request.UploadedByUserId));
        }

        if(string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("File name is required", nameof(request.FileName));
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new ArgumentException("Content type is required.", nameof(request.ContentType));
        }

        if (request.Content is null || request.Content.Length == 0)
        {
            throw new ArgumentException("File content is required.", nameof(request.Content));
        }

        if (request.Content.LongLength > MaxFileSizeBytes)
        {
            throw new ArgumentException("File size exceeds the 10 MB limit.", nameof(request.Content));
        }

        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("File type is not allowed.", nameof(request.FileName));
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
