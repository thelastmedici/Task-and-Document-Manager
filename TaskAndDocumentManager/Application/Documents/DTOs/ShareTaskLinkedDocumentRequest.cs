namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed class ShareTaskLinkedDocumentRequest
{
    public Guid DocumentId { get; init; }
    public Guid TaskId { get; init; }
    public Guid TargetUserId { get; init; }
    public Guid GrantedByUserId { get; init; }
}
