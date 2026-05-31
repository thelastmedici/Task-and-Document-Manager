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
        return ExecuteAsync(title, description, ownerId, null, cancellationToken);
    }

    public async Task<Guid> ExecuteAsync(
        string title,
        string description,
        Guid ownerId,
        DateTime? dueAtUtc = null,
        CancellationToken cancellationToken = default
        )
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        }

        var task = new TaskItem(title, description, ownerId, dueAtUtc);

        await _taskRepository.CreateAsync(task, cancellationToken);

        return task.Id;
    }
}
