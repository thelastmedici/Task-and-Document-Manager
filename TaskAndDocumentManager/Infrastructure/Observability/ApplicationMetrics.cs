using System.Diagnostics;
using System.Diagnostics.Metrics;
using TaskAndDocumentManager.Application.Common.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Observability;

public sealed class ApplicationMetrics : IApplicationMetrics, IDisposable
{
    public const string MeterName = "TaskAndDocumentManager";

    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly Counter<long> _documentUploadFailures;
    private readonly Counter<long> _documentUploadSuccesses;
    private readonly Histogram<long> _documentUploadSizeBytes;
    private readonly Counter<long> _loginFailures;
    private readonly Counter<long> _loginSuccesses;
    private readonly Counter<long> _requestErrors;
    private readonly Histogram<double> _requestDurationMilliseconds;
    private readonly Counter<long> _requests;
    private readonly Counter<long> _taskCreated;
    private readonly Counter<long> _usersRegistered;

    public ApplicationMetrics()
    {
        _requests = _meter.CreateCounter<long>("app.requests.total");
        _requestErrors = _meter.CreateCounter<long>("app.requests.errors.total");
        _requestDurationMilliseconds = _meter.CreateHistogram<double>("app.requests.duration.ms");
        _usersRegistered = _meter.CreateCounter<long>("app.users.registered.total");
        _loginSuccesses = _meter.CreateCounter<long>("app.auth.login.succeeded.total");
        _loginFailures = _meter.CreateCounter<long>("app.auth.login.failed.total");
        _taskCreated = _meter.CreateCounter<long>("app.tasks.created.total");
        _documentUploadSuccesses = _meter.CreateCounter<long>("app.documents.upload.succeeded.total");
        _documentUploadFailures = _meter.CreateCounter<long>("app.documents.upload.failed.total");
        _documentUploadSizeBytes = _meter.CreateHistogram<long>("app.documents.upload.size.bytes");
    }

    public void RecordRequest(string method, string route, int statusCode, double durationMilliseconds)
    {
        var tags = new TagList
        {
            { "method", method },
            { "route", route },
            { "status_code", statusCode }
        };

        _requests.Add(1, tags);
        _requestDurationMilliseconds.Record(durationMilliseconds, tags);

        if (statusCode >= 500)
        {
            _requestErrors.Add(1, tags);
        }
    }

    public void RecordUserRegistered(Guid userId, Guid workspaceId)
    {
        _usersRegistered.Add(1, WorkspaceTags(workspaceId, userId));
    }

    public void RecordLoginSucceeded(Guid userId, Guid workspaceId)
    {
        _loginSuccesses.Add(1, WorkspaceTags(workspaceId, userId));
    }

    public void RecordLoginFailed(Guid? userId, Guid? workspaceId)
    {
        var tags = new TagList
        {
            { "has_user_id", userId.HasValue },
            { "has_workspace_id", workspaceId.HasValue }
        };

        if (workspaceId.HasValue)
        {
            tags.Add("workspace_id", workspaceId.Value.ToString());
        }

        _loginFailures.Add(1, tags);
    }

    public void RecordTaskCreated(Guid taskId, Guid ownerId, Guid workspaceId)
    {
        var tags = WorkspaceTags(workspaceId, ownerId);
        tags.Add("task_id", taskId.ToString());
        _taskCreated.Add(1, tags);
    }

    public void RecordDocumentUploadSucceeded(Guid documentId, Guid ownerId, Guid workspaceId, long sizeInBytes)
    {
        var tags = WorkspaceTags(workspaceId, ownerId);
        tags.Add("document_id", documentId.ToString());
        _documentUploadSuccesses.Add(1, tags);
        _documentUploadSizeBytes.Record(sizeInBytes, tags);
    }

    public void RecordDocumentUploadFailed(Guid ownerId, Guid workspaceId, string reason)
    {
        var tags = WorkspaceTags(workspaceId, ownerId);
        tags.Add("reason", reason);
        _documentUploadFailures.Add(1, tags);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static TagList WorkspaceTags(Guid workspaceId, Guid userId)
    {
        return new TagList
        {
            { "workspace_id", workspaceId.ToString() },
            { "user_id", userId.ToString() }
        };
    }
}
