namespace TaskAndDocumentManager.Application.Tasks.DTOs;

public sealed record TaskListItemDto(
    Guid Id,
    string Title,
    string Description,
    Guid? AssignedToUserId,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsCompleted,
    DateTime? CompletedAt
    );
