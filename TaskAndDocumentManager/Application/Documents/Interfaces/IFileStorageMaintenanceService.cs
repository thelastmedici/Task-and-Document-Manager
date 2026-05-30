namespace TaskAndDocumentManager.Application.Documents.Interfaces;

public interface IFileStorageMaintenanceService
{
    Task<IReadOnlyCollection<string>> GetStoredFilePathsAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
