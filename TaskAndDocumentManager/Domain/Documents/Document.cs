namespace TaskAndDocumentManager.Domain.Documents;

public class Document
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeInBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAtUtc { get; private set; } = DateTime.UtcNow;
    public Guid? LinkedTaskId { get; private set; }

    protected Document()
    {
    }

    public Document(
        string fileName,
        string contentType,
        long sizeInBytes,
        string storagePath,
        Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Storage path is required.", nameof(storagePath));
        }

        if (uploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Uploaded by user ID is required.", nameof(uploadedByUserId));
        }

        if (sizeInBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Size cannot be negative.");
        }

        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        SizeInBytes = sizeInBytes;
        StoragePath = storagePath.Trim();
        UploadedByUserId = uploadedByUserId;
    }

    public void LinkToTask(Guid taskId)
    {
        if (taskId == Guid.Empty)
        {
            throw new ArgumentException("Task ID is required.", nameof(taskId));
        }

        LinkedTaskId = taskId;
    }
}
