using System.IO;

namespace TaskAndDocumentManager.Application.Documents.DTOs;

public sealed record DownloadDocumentResult(
    Stream Content,
    string ContentType,
    string FileName);
