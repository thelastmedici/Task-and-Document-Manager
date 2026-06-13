namespace TaskAndDocumentManager.Application.Audit.DTOs;

public sealed record AuditLogDto(
    Guid Id,
    Guid UserId,
    string Action,
    string EntityType,
    Guid EntityId,
    DateTime TimestampUtc);
