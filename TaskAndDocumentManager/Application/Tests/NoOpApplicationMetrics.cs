using TaskAndDocumentManager.Application.Common.Interfaces;

namespace TaskAndDocumentManager.Application.Tests;

public sealed class NoOpApplicationMetrics : IApplicationMetrics
{
    public void RecordRequest(string method, string route, int statusCode, double durationMilliseconds)
    {
    }

    public void RecordUserRegistered(Guid userId, Guid workspaceId)
    {
    }

    public void RecordLoginSucceeded(Guid userId, Guid workspaceId)
    {
    }

    public void RecordLoginFailed(Guid? userId, Guid? workspaceId)
    {
    }

    public void RecordTaskCreated(Guid taskId, Guid ownerId, Guid workspaceId)
    {
    }

    public void RecordDocumentUploadSucceeded(Guid documentId, Guid ownerId, Guid workspaceId, long sizeInBytes)
    {
    }

    public void RecordDocumentUploadFailed(Guid ownerId, Guid workspaceId, string reason)
    {
    }
}
