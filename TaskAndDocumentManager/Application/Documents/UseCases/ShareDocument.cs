using System.IO;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Documents;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class ShareDocument
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly INotificationRepository _notificationRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public ShareDocument(
        IAuditLogRepository auditLogRepository,
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository,
        INotificationDispatcher notificationDispatcher,
        INotificationRepository notificationRepository,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _auditLogRepository = auditLogRepository;
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
        _notificationDispatcher = notificationDispatcher;
        _notificationRepository = notificationRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task ExecuteAsync(
        ShareDocumentRequest request,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TargetUserId == Guid.Empty)
        {
            throw new ArgumentException("Target user ID is required.", nameof(request.TargetUserId));
        }

        if (request.GrantedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Granted by user ID is required.", nameof(request.GrantedByUserId));
        }

        if (request.WorkspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(request.WorkspaceId));
        }

        var document = await _documentRepository.GetByIdInWorkspaceAsync(
                request.DocumentId,
                request.WorkspaceId,
                cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (!isAdmin && document.OwnerId != request.GrantedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can share this document.");
        }

        if (request.TargetUserId == request.GrantedByUserId)
        {
            throw new InvalidOperationException("You cannot share a document with yourself.");
        }

        EnsureSharingUserIsWorkspaceMember(request.WorkspaceId, request.GrantedByUserId);
        EnsureTargetUserIsWorkspaceMember(request.WorkspaceId, request.TargetUserId);

        var access = new DocumentAccess(
            request.DocumentId,
            request.TargetUserId,
            request.GrantedByUserId,
            request.WorkspaceId);
        await _documentAccessRepository.GrantAccessAsync(access, cancellationToken);
        var notification = new Notification(
            request.TargetUserId,
            request.WorkspaceId,
            "Document shared with you",
            $"{document.OriginalFileName} was shared with you.");
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _notificationDispatcher.DispatchCreatedAsync(notification, cancellationToken);
        await _auditLogRepository.AddAsync(
            new AuditLog(
                request.GrantedByUserId,
                AuditActions.DocumentShared,
                nameof(Document),
                request.DocumentId,
                request.WorkspaceId),
            cancellationToken);
    }

    private void EnsureSharingUserIsWorkspaceMember(Guid workspaceId, Guid userId)
    {
        if (!_workspaceMemberRepository.IsMember(workspaceId, userId))
        {
            throw new UnauthorizedAccessException("You do not have access to this workspace.");
        }
    }

    private void EnsureTargetUserIsWorkspaceMember(Guid workspaceId, Guid userId)
    {
        if (!_workspaceMemberRepository.IsMember(workspaceId, userId))
        {
            throw new InvalidOperationException("The target user must be a member of this workspace.");
        }
    }
}
