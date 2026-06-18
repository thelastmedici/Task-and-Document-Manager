namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed record DocumentQuery(
    string? SearchTerm = null,
    string? ContentType = null,
    DateTime? UploadedFromUtc = null,
    DateTime? UploadedToUtc = null,
    int PageNumber = 1,
    int PageSize = 20,
    Guid? WorkspaceId = null)
{
    public static DocumentQuery Empty { get; } = new();
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;
}
