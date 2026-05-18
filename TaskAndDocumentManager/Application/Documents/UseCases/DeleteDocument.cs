using System.IO;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class DeleteDocument
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;

    public DeleteDocument(
        IAuditLogRepository auditLogRepository,
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService)
    {
        _auditLogRepository = auditLogRepository;
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

        if (document.OwnerId != requestedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can delete this document.");
        }

        await _fileStorageService.DeleteAsync(document.StoragePath, cancellationToken);
        await _documentRepository.DeleteAsync(documentId, cancellationToken);
        await _auditLogRepository.AddAsync(
            new AuditLog(
                requestedByUserId,
                AuditActions.DocumentDeleted,
                nameof(Document),
                documentId),
            cancellationToken);
    }
}
