using TaskAndDocumentManager.Application.BackgroundJobs;
using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class CleanupOrphanedDocumentFiles : IBackgroundJob
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageMaintenanceService _fileStorageMaintenanceService;

    public CleanupOrphanedDocumentFiles(
        IDocumentRepository documentRepository,
        IFileStorageMaintenanceService fileStorageMaintenanceService)
    {
        _documentRepository = documentRepository;
        _fileStorageMaintenanceService = fileStorageMaintenanceService;
    }

    public string Name => nameof(CleanupOrphanedDocumentFiles);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetAllAsync(cancellationToken);
        var knownStoragePaths = documents
            .Select(document => document.StoragePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var storedFilePaths = await _fileStorageMaintenanceService.GetStoredFilePathsAsync(cancellationToken);

        foreach (var storedFilePath in storedFilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!knownStoragePaths.Contains(storedFilePath))
            {
                await _fileStorageMaintenanceService.DeleteAsync(storedFilePath, cancellationToken);
            }
        }
    }
}
