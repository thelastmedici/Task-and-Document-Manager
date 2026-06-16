using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAndDocumentManager.Application.Common.DTOs;
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

    public async Task<PaginatedResult<TaskListItemDto>> ExecuteAsync(
        TaskQuery query,
        Guid actorId,
        bool isAdmin,
        bool isManager,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var scopedQuery = ApplyAccessScope(normalizedQuery, actorId, isAdmin, isManager);

        var tasks = await _taskRepository.SearchTasksAsync(scopedQuery, cancellationToken);
        var totalCount = await _taskRepository.CountTasksAsync(scopedQuery, cancellationToken);

        var items = ApplySort(tasks, scopedQuery)
            .Select(task => new TaskListItemDto(
                task.Id,
                task.Title,
                task.Description,
                task.AssignedToUserId,
                task.OwnerId,
                task.CreatedAt,
                task.UpdatedAt,
                task.DueAtUtc,
                task.DeadlineReminderSentAtUtc,
                task.Priority,
                task.IsCompleted,
                task.CompletedAt))
                .ToList();

        return new PaginatedResult<TaskListItemDto>(
            items,
            totalCount,
            scopedQuery.PageNumber,
            scopedQuery.PageSize);
    }

    private static TaskQuery NormalizeQuery(TaskQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

        var pageSize = query.PageSize < 1
            ? TaskQuery.DefaultPageSize
            : Math.Min(query.PageSize, TaskQuery.MaxPageSize);

        var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
            ? null
            : query.SearchTerm.Trim();

        if (query.DueAfterUtc.HasValue && query.DueBeforeUtc.HasValue &&
            query.DueAfterUtc.Value > query.DueBeforeUtc.Value)
        {
            throw new ArgumentException("Due after date cannot be later than due before date.", nameof(query));
        }

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            throw new ArgumentException("Status filter is not supported.", nameof(query));
        }

        if (query.Priority.HasValue && !Enum.IsDefined(query.Priority.Value))
        {
            throw new ArgumentException("Priority filter is not supported.", nameof(query));
        }

        if (!Enum.IsDefined(query.SortBy))
        {
            throw new ArgumentException("Sort field is not supported.", nameof(query));
        }

        if (!Enum.IsDefined(query.SortDirection))
        {
            throw new ArgumentException("Sort direction is not supported.", nameof(query));
        }

        var isCompleted = query.Status switch
        {
            TaskStatusFilter.Pending => false,
            TaskStatusFilter.Completed => true,
            _ => query.IsCompleted
        };

        return query with
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsCompleted = isCompleted
        };
    }

    private static TaskQuery ApplyAccessScope(
        TaskQuery query,
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
            return query with { IncludeAssignedTasks = false };
        }

        return query with
        {
            OwnerId = actorId,
            IncludeAssignedTasks = isManager
        };
    }

    private static IOrderedEnumerable<Domain.Tasks.TaskItem> ApplySort(
        IEnumerable<Domain.Tasks.TaskItem> tasks,
        TaskQuery query)
    {
        return (query.SortBy, query.SortDirection) switch
        {
            (TaskSortBy.DueAt, SortDirection.Ascending) => tasks
                .OrderBy(task => task.DueAtUtc ?? DateTime.MaxValue)
                .ThenByDescending(task => task.CreatedAt),
            (TaskSortBy.DueAt, SortDirection.Descending) => tasks
                .OrderByDescending(task => task.DueAtUtc ?? DateTime.MinValue)
                .ThenByDescending(task => task.CreatedAt),
            (TaskSortBy.Priority, SortDirection.Ascending) => tasks
                .OrderBy(task => task.Priority)
                .ThenByDescending(task => task.CreatedAt),
            (TaskSortBy.Priority, SortDirection.Descending) => tasks
                .OrderByDescending(task => task.Priority)
                .ThenByDescending(task => task.CreatedAt),
            (TaskSortBy.CreatedAt, SortDirection.Ascending) => tasks.OrderBy(task => task.CreatedAt),
            _ => tasks.OrderByDescending(task => task.CreatedAt)
        };
    }
}
