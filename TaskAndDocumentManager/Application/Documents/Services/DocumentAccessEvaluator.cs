using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Documents.Services;

public class DocumentAccessEvaluator
{
    private readonly IDocumentAccessRepository _documentAccessRepository;
    private readonly ITaskRepository _taskRepository;

    public DocumentAccessEvaluator(
        IDocumentAccessRepository documentAccessRepository,
        ITaskRepository taskRepository)
    {
        _documentAccessRepository = documentAccessRepository;
        _taskRepository = taskRepository;
    }

    public async Task<bool> HasAccessAsync(
        Document document,
        Guid requestedByUserId,
        bool allowTaskParticipationAccess,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.OwnerId == requestedByUserId)
        {
            return true;
        }

        if (await _documentAccessRepository.HasAccessAsync(document.Id, requestedByUserId, cancellationToken))
        {
            return true;
        }

        if (!allowTaskParticipationAccess || !document.LinkedTaskId.HasValue)
        {
            return false;
        }

        var task = await _taskRepository.GetByIdAsync(document.LinkedTaskId.Value, cancellationToken);

        return task is not null && IsTaskParticipant(task, requestedByUserId);
    }

    private static bool IsTaskParticipant(TaskItem task, Guid userId)
    {
        return task.OwnerId == userId || task.AssignedToUserId == userId;
    }
}
