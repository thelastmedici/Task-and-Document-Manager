using System.IO;
namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed class UploadDocumentRequest
{
    public required string FileName {get; init;}
    public required string ContentType {get; init;}
    public required Stream Content {get; init;}
    public long SizeInBytes {get; init;}
    public Guid UploadedByUserId {get; init;}
    public Guid WorkspaceId { get; init; }
}
