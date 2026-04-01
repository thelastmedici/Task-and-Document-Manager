using System;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.Interfaces;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class UpdateTask
{

    private readonly ITaskRepository _taskRepository;


    public UpdateTask(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task ExecuteAsync(Guid taskId,string title,string description,CancellationToken cancellationToken= default)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            throw new InvalidOperationException("Task not found");

        }

        task.UpdateTask(title, description);

        await _taskRepository.UpdateAsync(task, cancellationToken);
    }
}