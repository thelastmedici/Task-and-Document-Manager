using TaskAndDocumentManager.Application.Documents.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Documents;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;

    public LocalFileStorageService()
    {
        _storageRoot = Path.Combine(AppContext.BaseDirectory, "storage", "documents");
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var storedFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_storageRoot, storedFileName);

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
