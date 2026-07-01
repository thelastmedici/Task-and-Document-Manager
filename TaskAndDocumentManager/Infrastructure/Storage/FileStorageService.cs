using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Storage;

public class FileStorageService : IFileStorageService, IFileStorageMaintenanceService
{
    private readonly string _storageRoot;
    private readonly FileStorageOptions _options;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService()
        : this(Options.Create(new FileStorageOptions()), NullLogger<FileStorageService>.Instance)
    {
    }

    public FileStorageService(
        IOptions<FileStorageOptions> options,
        ILogger<FileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _storageRoot = Path.Combine(AppContext.BaseDirectory, "storage", "uploads");
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveAsync(
        Guid uploadByUserId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if(uploadByUserId == Guid.Empty)
        {
            throw new ArgumentException("Upload by user ID is required.", nameof(uploadByUserId));
        }
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }
        if(content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        using var timeoutSource = CreateTimeoutSource(cancellationToken, out var operationToken);

        var safeFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        var userFolder = Path.Combine(_storageRoot, uploadByUserId.ToString());
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(userFolder, storedFileName);

        try
        {
            operationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(userFolder);

            await using var output = File.Create(fullPath);
            await content.CopyToAsync(output, operationToken);
        }
        catch (OperationCanceledException ex)
        {
            DeletePartialFile(fullPath);

            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogWarning(
                ex,
                "Timed out while saving uploaded file for user {UserId}.",
                uploadByUserId);

            throw new TimeoutException("File storage operation timed out.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DeletePartialFile(fullPath);
            _logger.LogError(
                ex,
                "Failed to save uploaded file for user {UserId}.",
                uploadByUserId);
            throw;
        }

        return fullPath;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CreateTimeoutSource(cancellationToken, out var operationToken);

        try
        {
            operationToken.ThrowIfCancellationRequested();

            if (!File.Exists(storagePath))
            {
                throw new FileNotFoundException("Stored document was not found.", storagePath);
            }

            Stream stream = File.OpenRead(storagePath);
            return Task.FromResult(stream);
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogWarning(ex, "Timed out while opening stored file.");
            throw new TimeoutException("File storage operation timed out.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to open stored file.");
            throw;
        }
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CreateTimeoutSource(cancellationToken, out var operationToken);

        try
        {
            operationToken.ThrowIfCancellationRequested();

            if (File.Exists(storagePath))
            {
                File.Delete(storagePath);
            }
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogWarning(ex, "Timed out while deleting stored file.");
            throw new TimeoutException("File storage operation timed out.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to delete stored file.");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<string>> GetStoredFilePathsAsync(CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CreateTimeoutSource(cancellationToken, out var operationToken);

        try
        {
            operationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(_storageRoot))
            {
                return Task.FromResult((IReadOnlyCollection<string>)Array.Empty<string>());
            }

            var filePaths = Directory
                .EnumerateFiles(_storageRoot, "*", SearchOption.AllDirectories)
                .ToList();

            return Task.FromResult((IReadOnlyCollection<string>)filePaths);
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogWarning(ex, "Timed out while listing stored files.");
            throw new TimeoutException("File storage operation timed out.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to list stored files.");
            throw;
        }
    }

    private CancellationTokenSource? CreateTimeoutSource(
        CancellationToken cancellationToken,
        out CancellationToken operationToken)
    {
        if (_options.OperationTimeout <= TimeSpan.Zero)
        {
            operationToken = cancellationToken;
            return null;
        }

        var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_options.OperationTimeout);
        operationToken = timeoutSource.Token;
        return timeoutSource;
    }

    private void DeletePartialFile(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to delete partial uploaded file after storage failure.");
        }
    }
}
