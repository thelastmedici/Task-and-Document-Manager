namespace TaskAndDocumentManager.Domain.Entities;

public class Document
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeInBytes { get; private set; }

    public string StoragePath { get; private set; } = string.Empty;

    public Guid OwnerId { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public DateTime UploadedAtUtc { get; private set; } = DateTime.UtcNow;

    public Guid? LinkedTaskId { get; private set; }

    protected Document()
    {
    }

    public Document(
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string storagePath,
        Guid ownerId)
        : this(originalFileName, contentType, sizeInBytes, storagePath, ownerId, ownerId)
    {
    }

    public Document(
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string storagePath,
        Guid ownerId,
        Guid workspaceId)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException("Storage path is required.", nameof(storagePath));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        if (sizeInBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Size cannot be negative.");
        }

        OriginalFileName = originalFileName.Trim();
        ContentType = contentType.Trim();
        SizeInBytes = sizeInBytes;
        StoragePath = storagePath.Trim();
        OwnerId = ownerId;
        WorkspaceId = workspaceId;
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
