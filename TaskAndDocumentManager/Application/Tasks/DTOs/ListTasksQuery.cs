using System;
namespace TaskAndDocumentManager.Application.Tasks.DTOs;


public sealed record ListTasksQuery(
    int PageNumber = 1,
    int PageSize = 50,
    string? SearchTerm = null,
    bool? IsCompleted = null,
    Guid? AssignedToUserId = null
    )
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;
}
