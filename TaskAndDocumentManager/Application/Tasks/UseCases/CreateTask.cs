using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class CreateTask
{
    private readonly ITaskRepository _taskRepository;

    public CreateTask(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task<Guid> ExecuteAsync(
        string title,
        string description,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(title, description, ownerId, ownerId, dueAtUtc: null, cancellationToken: cancellationToken);
    }

    public Task<Guid> ExecuteAsync(
        string title,
        string description,
        Guid ownerId,
        DateTime? dueAtUtc = null,
        TaskPriority priority = TaskPriority.Medium,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(title, description, ownerId, ownerId, dueAtUtc, priority, cancellationToken);
    }

    public async Task<Guid> ExecuteAsync(
        string title,
        string description,
        Guid ownerId,
        Guid workspaceId,
        DateTime? dueAtUtc = null,
        TaskPriority priority = TaskPriority.Medium,
        CancellationToken cancellationToken = default
        )
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        var task = new TaskItem(title, description, ownerId, workspaceId, dueAtUtc, priority);

        await _taskRepository.CreateAsync(task, cancellationToken);

        return task.Id;
    }
}
