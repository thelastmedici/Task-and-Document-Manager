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
}

}