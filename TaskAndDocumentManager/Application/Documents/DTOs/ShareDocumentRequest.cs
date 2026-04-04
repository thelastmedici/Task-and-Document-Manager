namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed class ShareDocumentRequest
{
    public Guid DocumentId { get; init; }
    public Guid TargetUserId { get; init; }
    public Guid GrantedByUserId { get; init; }
}
