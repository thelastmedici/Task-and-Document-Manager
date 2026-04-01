using System;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.Dtos;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tasks.Interfaces
{
    public interface ITaskRepository
    {
        Task CreateAsync(TaskItem task, CancellationToken cancellationToken = default);

        Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default);

       

        Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

          // for scalable querying
         Task<IReadOnlyList<TaskItem>> SearchAsync(
            ListTasksQuery query,
            CancellationToken cancellationToken = default);
    }
}
