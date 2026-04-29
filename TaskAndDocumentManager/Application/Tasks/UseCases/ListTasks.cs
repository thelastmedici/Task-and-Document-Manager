using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Tasks.DTOs;
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
        Guid actorId,
        bool isAdmin,
        bool isManager,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var scopedQuery = ApplyAccessScope(normalizedQuery, actorId, isAdmin, isManager);

        var tasks = await _taskRepository.SearchAsync(scopedQuery, cancellationToken);

        return tasks
            .OrderByDescending(task => task.CreatedAt)
            .Select(task => new TaskListItemDto(
                task.Id,
                task.Title,
                task.Description,
                task.AssignedToUserId,
                task.OwnerId,
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

    private static ListTasksQuery ApplyAccessScope(
        ListTasksQuery query,
        Guid actorId,
        bool isAdmin,
        bool isManager)
    {
        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("Actor ID is required.", nameof(actorId));
        }

        if (isAdmin)
        {
            return query with
            {
                OwnerId = null,
                IncludeAssignedTasks = false
            };
        }

        return query with
        {
            OwnerId = actorId,
            IncludeAssignedTasks = isManager
        };
    }
}
