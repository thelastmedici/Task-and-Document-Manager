using System;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tasks.DTOs;

public sealed record TaskQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    bool? IsCompleted = null,
    Guid? AssignedToUserId = null,
    Guid? OwnerId = null,
    bool IncludeAssignedTasks = false,
    TaskStatusFilter? Status = null,
    TaskPriority? Priority = null,
    DateTime? DueAfterUtc = null,
    DateTime? DueBeforeUtc = null,
    TaskSortBy SortBy = TaskSortBy.CreatedAt,
    SortDirection SortDirection = SortDirection.Descending
    )
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;
}
