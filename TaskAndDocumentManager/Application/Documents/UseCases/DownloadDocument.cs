using System.IO;
using Microsoft.Extensions.Logging;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class DownloadDocument
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<DownloadDocument> _logger;

    public DownloadDocument(
        IAuditLogRepository auditLogRepository,
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository,
        IFileStorageService fileStorageService,
        ILogger<DownloadDocument> logger)
    {
        _auditLogRepository = auditLogRepository;
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<DownloadDocumentResult> ExecuteAsync(
        Guid documentId,
        Guid requestedByUserId,
        Guid workspaceId,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requested by user ID is required.", nameof(requestedByUserId));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (document.WorkspaceId != workspaceId)
        {
            throw new FileNotFoundException("Document not found.");
        }

        if (!isAdmin && document.OwnerId != requestedByUserId)
        {
            var hasSharedAccess = await _documentAccessRepository.HasAccessAsync(
                document.Id,
                requestedByUserId,
                cancellationToken);

            if (!hasSharedAccess)
            {
                throw new UnauthorizedAccessException("You do not have access to this document.");
            }
        }

        try
        {
            var stream = await _fileStorageService.OpenReadAsync(document.StoragePath, cancellationToken);

            await _auditLogRepository.AddAsync(
                new AuditLog(
                    requestedByUserId,
                    AuditActions.DocumentDownloaded,
                    nameof(Document),
                    document.Id),
                cancellationToken);

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
