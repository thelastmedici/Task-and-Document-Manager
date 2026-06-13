using System;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.DTOs;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tasks.Interfaces
{
    public interface ITaskRepository
    {
        Task CreateAsync(TaskItem task, CancellationToken cancellationToken = default);

        Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default);

        Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TaskItem>> SearchAsync(
            TaskQuery query,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            TaskQuery query,
            CancellationToken cancellationToken = default);
    }
}
