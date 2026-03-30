using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.Dtos;
using TaskAndDocumentManager.Application.Tasks.Interfaces;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class ListTasks
{
    private readonly ITaskRepository _taskRepository;

    public ListTasks(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<IReadOnlyList<TaskListItemDto>> ExecuteAsync(
        ListTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);

        var tasks = await _taskRepository.SearchAsync(normalizedQuery, cancellationToken);

        return tasks
            .OrderByDescending(task => task.CreatedAt)
            .Select(task => new TaskListItemDto(
                task.Id,
                task.Title,
                task.Description,
                task.AssignedToUserId,
                task.CreatedByUserId,
                task.CreatedAt,
                task.UpdatedAt,
                task.IsCompleted,
                task.CompletedAt))
                .ToList();
    }

    private static ListTasksQuery NormalizeQuery(ListTasksQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

        var pageSize = query.PageSize < 1
            ? ListTasksQuery.DefaultPageSize
            : Math.Min(query.PageSize, ListTasksQuery.MaxPageSize);

        var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
            ? null
            : query.SearchTerm.Trim();

        return query with
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm
        };
    }
}