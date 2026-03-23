using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskAndDocumentManager.Domain.Entities;

namespace Application.TodoTask.Interfaces
{
   public interface ITaskRepository
{
   Task<TaskItem> CreateAsync(TaskItem task, CancellationToken cancellationToken=default);// method is asynchronous and returns a TaskItem when finished

   Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken=default);

   Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken=default);

   Task UpdateAsync(TaskItem task, CancellationToken cancellationToken=default); // update existing task

   Task DeleteAsync(Guid id, CancellationToken cancellationToken=default); // delete completed task
}

}