using System.IO;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class UploadDocument
{
    private static readonly Dictionary<string, string[]> AllowedFileTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".png"] = ["image/png"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".docx"] =
        [
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ]
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

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

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("File name is required.", nameof(request.FileName));
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new ArgumentException("Content type is required.", nameof(request.ContentType));
        }

        if (request.Content is null)
        {
            throw new ArgumentException("File content is required.", nameof(request.Content));
        }

        if (request.SizeInBytes <= 0)
        {
            throw new ArgumentException("File content is required.", nameof(request.SizeInBytes));
        }

        if (request.SizeInBytes > MaxFileSizeBytes)
        {
            throw new ArgumentException("File size exceeds the 20 MB limit.", nameof(request.SizeInBytes));
        }

        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedFileTypes.TryGetValue(extension, out var allowedContentTypes))
        {
            throw new ArgumentException("File type is not allowed.", nameof(request.FileName));
        }

        if (!allowedContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("File content type is not allowed for the given file extension.", nameof(request.ContentType));
        }

        var storagePath = await _fileStorageService.SaveAsync(
            request.UploadedByUserId,
            request.FileName,
            request.Content,
            cancellationToken);

        var document = new Document(
            request.FileName,
            request.ContentType,
            request.SizeInBytes,
            storagePath,
            request.UploadedByUserId);

        try
        {
            await _documentRepository.AddAsync(document, cancellationToken);
        }
        catch
        {
            try
            {
                await _fileStorageService.DeleteAsync(storagePath, cancellationToken);
            }
            catch
            {
                // Preserve the original persistence failure if cleanup also fails.
            }

            throw;
        }

        return document.Id;
    }
}
