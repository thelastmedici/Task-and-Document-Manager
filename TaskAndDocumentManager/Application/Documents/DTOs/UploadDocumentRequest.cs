namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed class UploadDocumentRequest
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Content { get; init; }
    public Guid UploadedByUserId { get; init; }
}
