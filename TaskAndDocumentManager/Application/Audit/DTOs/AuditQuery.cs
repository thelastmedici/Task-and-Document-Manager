namespace TaskAndDocumentManager.Application.Audit.DTOs;

public sealed record AuditQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? UserId = null,
    string? Action = null,
    DateTime? TimestampFromUtc = null,
    DateTime? TimestampToUtc = null)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;
}
