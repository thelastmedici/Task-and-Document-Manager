using System.IO;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Documents;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class ShareTaskLinkedDocument
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;
    private readonly ITaskRepository _taskRepository;

    public ShareTaskLinkedDocument(
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository,
        ITaskRepository taskRepository)
    {
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
        _taskRepository = taskRepository;
    }

    public async Task ExecuteAsync(
        ShareTaskLinkedDocumentRequest request,
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

        if (request.TaskId == Guid.Empty)
        {
            throw new ArgumentException("Task ID is required.", nameof(request.TaskId));
        }

        if (request.TargetUserId == request.GrantedByUserId)
        {
            throw new InvalidOperationException("You cannot share a document with yourself.");
        }

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (document.UploadedByUserId != request.GrantedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can share this document.");
        }

        if (document.LinkedTaskId is null)
        {
            throw new InvalidOperationException("Document is not linked to a task.");
        }

        if (document.LinkedTaskId.Value != request.TaskId)
        {
            throw new InvalidOperationException("Document is linked to a different task.");
        }

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new FileNotFoundException("Task not found.");

        if (!IsTaskParticipant(task, request.GrantedByUserId))
        {
            throw new UnauthorizedAccessException("Only a task participant can share a task-linked document.");
        }

        if (!IsTaskParticipant(task, request.TargetUserId))
        {
            throw new InvalidOperationException("Target user must be a participant in the linked task.");
        }

        var access = new DocumentAccess(request.DocumentId, request.TargetUserId, request.GrantedByUserId);
        await _documentAccessRepository.GrantAccessAsync(access, cancellationToken);
    }

    private static bool IsTaskParticipant(TaskItem task, Guid userId)
    {
        return task.CreatedByUserId == userId || task.AssignedToUserId == userId;
    }
}
