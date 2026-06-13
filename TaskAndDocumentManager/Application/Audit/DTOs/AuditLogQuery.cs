namespace TaskAndDocumentManager.Application.Audit.DTOs;

public sealed record AuditLogQuery(
    int PageNumber = 1,
    int PageSize = 20)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;
}
