using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tasks.DTOs;

public sealed record TaskListItemDto(
    Guid Id,
    string Title,
    string Description,
    Guid? AssignedToUserId,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DueAtUtc,
    DateTime? DeadlineReminderSentAtUtc,
    TaskPriority Priority,
    bool IsCompleted,
    DateTime? CompletedAt
    );
