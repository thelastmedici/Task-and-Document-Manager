namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed class LinkDocumentToTaskRequest
{
    public Guid DocumentId { get; init; }
    public Guid TaskId { get; init; }
    public Guid RequestedByUserId { get; init; }
}
