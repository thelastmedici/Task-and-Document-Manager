namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed record UploadDocumentResponse(
    Guid Id,
    string Message);
