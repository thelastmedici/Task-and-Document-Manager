using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class AssignTask
{
    private readonly ITaskRepository _taskRepository;

    public AssignTask(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task ExecuteAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        TaskItem? task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);

        if (task == null)
        {
            throw new InvalidOperationException("Task was not found.");
        }

        task.AssignTask(userId);

        await _taskRepository.UpdateAsync(task, cancellationToken);
    }
}
