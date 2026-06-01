using System;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tasks.DTOs;


public sealed record ListTasksQuery(
    int PageNumber = 1,
    int PageSize = 50,
    string? SearchTerm = null,
    bool? IsCompleted = null,
    Guid? AssignedToUserId = null,
    Guid? OwnerId = null,
    bool IncludeAssignedTasks = false,
    TaskStatusFilter? Status = null,
    TaskPriority? Priority = null,
    DateTime? DueFromUtc = null,
    DateTime? DueToUtc = null,
    TaskSortBy SortBy = TaskSortBy.CreatedAt,
    SortDirection SortDirection = SortDirection.Descending
    )
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
}
