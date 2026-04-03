using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.Interfaces;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class DeleteTask
{
    private readonly ITaskRepository _taskRepository;

    public DeleteTask(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task ExecuteAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);

        if (task == null)
        {
            throw new FileNotFoundException("Task not found");
        }

        await _taskRepository.DeleteAsync(task.Id, cancellationToken);
    }
}
