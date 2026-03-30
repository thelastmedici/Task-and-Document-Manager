namespace TaskAndDocumentManager.Application.Tasks.Dtos;

public sealed record TaskListItemDto(
    Guid Id,
    string Title,
    string Description,
    Guid? AssignedToUserId,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsCompleted,
    DateTime? CompletedAt
    );