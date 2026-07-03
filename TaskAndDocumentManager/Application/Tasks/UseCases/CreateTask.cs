using Microsoft.Extensions.Logging;
using TaskAndDocumentManager.Application.Common.Interfaces;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class CreateTask
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<CreateTask> _logger;
    private readonly IApplicationMetrics _metrics;

    public CreateTask(
        ITaskRepository taskRepository,
        ILogger<CreateTask> logger,
        IApplicationMetrics metrics)
    {
        _taskRepository = taskRepository;
        _logger = logger;
        _metrics = metrics;
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

        _logger.LogInformation(
            "Task {TaskId} created by user {OwnerId} in workspace {WorkspaceId}.",
            task.Id,
            ownerId,
            workspaceId);
        _metrics.RecordTaskCreated(task.Id, ownerId, workspaceId);

        return task.Id;
    }
}
