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

    public async Task<Guid> ExecuteAsync(
        string title,
        string description,
        Guid createdByUserId,
        CancellationToken cancellationToken = default
        )
    {
        var task = new TaskItem(title, description, createdByUserId);

        await _taskRepository.CreateAsync(task, cancellationToken);

        return task.Id;
    }
}
