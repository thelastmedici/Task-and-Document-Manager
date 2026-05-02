using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Documents;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;

    public LocalFileStorageService()
    {
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

        var safeFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        var userFolder = Path.Combine(_storageRoot, uploadByUserId.ToString());
        Directory.CreateDirectory(userFolder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(userFolder, storedFileName);

        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, cancellationToken);

        return fullPath;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(storagePath))
        {
            throw new FileNotFoundException("Stored document was not found.", storagePath);
        }

        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }

        return Task.CompletedTask;
    }
}
