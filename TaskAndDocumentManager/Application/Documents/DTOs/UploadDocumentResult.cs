namespace TaskAndDocumentManager.Application.Documents.DTOs;
public sealed record UploadDocumentResult(
    Guid DocumentId,
    string FileName
);