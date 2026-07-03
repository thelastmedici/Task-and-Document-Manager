using Microsoft.AspNetCore.Http;
using TaskAndDocumentManager.Api.Middleware;
using TaskAndDocumentManager.Application.Common.Interfaces;

namespace TaskAndDocumentManager.Application.Tests.Api.Middleware;

public class RequestMetricsMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldRecordRequestDurationAndStatusCode()
    {
        var metrics = new CapturingMetrics();
        var middleware = new RequestMetricsMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/tasks";

        await middleware.InvokeAsync(context, metrics);

        Assert.Equal(HttpMethods.Get, metrics.Method);
        Assert.Equal("/api/v1/tasks", metrics.Route);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, metrics.StatusCode);
        Assert.True(metrics.DurationMilliseconds >= 0);
    }

    private sealed class CapturingMetrics : IApplicationMetrics
    {
        public string Method { get; private set; } = string.Empty;
        public string Route { get; private set; } = string.Empty;
        public int StatusCode { get; private set; }
        public double DurationMilliseconds { get; private set; }

        public void RecordRequest(string method, string route, int statusCode, double durationMilliseconds)
        {
            Method = method;
            Route = route;
            StatusCode = statusCode;
            DurationMilliseconds = durationMilliseconds;
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
}
