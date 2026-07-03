namespace TaskAndDocumentManager.Application.Common.Interfaces;

public interface IApplicationMetrics
{
    void RecordRequest(string method, string route, int statusCode, double durationMilliseconds);
    void RecordUserRegistered(Guid userId, Guid workspaceId);
    void RecordLoginSucceeded(Guid userId, Guid workspaceId);
    void RecordLoginFailed(Guid? userId, Guid? workspaceId);
    void RecordTaskCreated(Guid taskId, Guid ownerId, Guid workspaceId);
    void RecordDocumentUploadSucceeded(Guid documentId, Guid ownerId, Guid workspaceId, long sizeInBytes);
    void RecordDocumentUploadFailed(Guid ownerId, Guid workspaceId, string reason);
}
