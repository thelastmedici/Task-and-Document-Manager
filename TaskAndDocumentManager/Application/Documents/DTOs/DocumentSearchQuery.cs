namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed record DocumentSearchQuery(
    string? SearchTerm = null,
    string? ContentType = null,
    DateTime? UploadedFromUtc = null,
    DateTime? UploadedToUtc = null)
{
    public static DocumentSearchQuery Empty { get; } = new();
}
