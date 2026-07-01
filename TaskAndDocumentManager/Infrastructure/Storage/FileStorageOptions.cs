namespace TaskAndDocumentManager.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
