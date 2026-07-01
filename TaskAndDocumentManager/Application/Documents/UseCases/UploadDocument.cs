using System.IO;
using Microsoft.Extensions.Logging;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class UploadDocument
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAllowedDocumentTypeCatalog _allowedDocumentTypeCatalog;
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<UploadDocument> _logger;

    public UploadDocument(
        IAuditLogRepository auditLogRepository,
        IAllowedDocumentTypeCatalog allowedDocumentTypeCatalog,
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        ILogger<UploadDocument> logger)
    {
        _auditLogRepository = auditLogRepository;
        _allowedDocumentTypeCatalog = allowedDocumentTypeCatalog;
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<UploadDocumentResult> ExecuteAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Uploaded by user ID is required.", nameof(request.UploadedByUserId));
        }

        if (request.WorkspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(request.WorkspaceId));
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

        if (!_allowedDocumentTypeCatalog.IsAllowedExtension(extension))
        {
            throw new ArgumentException("File type is not allowed.", nameof(request.FileName));
        }

        if (!_allowedDocumentTypeCatalog.IsAllowedContentType(extension, request.ContentType))
        {
            throw new ArgumentException("File content type is not allowed for the given file extension.", nameof(request.ContentType));
        }

        string storagePath;

        try
        {
            storagePath = await _fileStorageService.SaveAsync(
                request.UploadedByUserId,
                request.FileName,
                request.Content,
                cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
        {
            _logger.LogError(
                ex,
                "Failed to store uploaded document for user {UserId} in workspace {WorkspaceId}.",
                request.UploadedByUserId,
                request.WorkspaceId);

            throw new InvalidOperationException("The document could not be uploaded. Please try again.", ex);
        }

        var document = new Document(
            request.FileName,
            request.ContentType,
            request.SizeInBytes,
            storagePath,
            request.UploadedByUserId,
            request.WorkspaceId);

        try
        {
            await _documentRepository.AddAsync(document, cancellationToken);
        }
        catch (Exception persistenceException)
        {
            try
            {
                await _fileStorageService.DeleteAsync(storagePath, cancellationToken);
            }
            catch (Exception cleanupException)
            {
                _logger.LogWarning(
                    cleanupException,
                    "Failed to clean up stored file after document metadata persistence failed for user {UserId} in workspace {WorkspaceId}.",
                    request.UploadedByUserId,
                    request.WorkspaceId);
                // Preserve the original persistence failure if cleanup also fails.
            }

            _logger.LogError(
                persistenceException,
                "Document metadata persistence failed after file storage succeeded for user {UserId} in workspace {WorkspaceId}.",
                request.UploadedByUserId,
                request.WorkspaceId);

            throw;
        }

        await _auditLogRepository.AddAsync(
            new AuditLog(
                request.UploadedByUserId,
                AuditActions.DocumentUploaded,
                nameof(Document),
                document.Id,
                request.WorkspaceId),
            cancellationToken);

        return new UploadDocumentResult(
            document.Id,
            document.OriginalFileName
        );
    }
}
