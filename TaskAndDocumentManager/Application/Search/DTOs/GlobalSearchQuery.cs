namespace TaskAndDocumentManager.Application.Search.DTOs;

public sealed record GlobalSearchQuery(
    string? SearchTerm,
    int PageNumber = 1,
    int PageSize = 10)
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;
}
