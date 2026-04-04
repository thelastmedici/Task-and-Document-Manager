namespace TaskAndDocumentManager.Application.Documents.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
