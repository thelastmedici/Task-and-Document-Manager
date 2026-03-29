using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class ListTasks
{
    private readonly ITaskRepository _taskRepository;

    public ListTasks(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task<IReadOnlyList<TaskItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _taskRepository.GetAllAsync(cancellationToken);
    }
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class ListTasks
{
    private readonly ITaskRepository _taskRepository;

    public ListTasks(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task<IReadOnlyList<TaskItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _taskRepository.GetAllAsync(cancellationToken);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class ListTasks
{
    private readonly ITaskRepository _taskRepository;

    public ListTasks(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task<IReadOnlyList<TaskItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _taskRepository.GetAllAsync(cancellationToken);
    }
}