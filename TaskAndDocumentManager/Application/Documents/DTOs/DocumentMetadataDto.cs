namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed record DocumentMetadataDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeInBytes,
    Guid UploadedByUserId,
    DateTime UploadedAtUtc,
    Guid? LinkedTaskId);
