using System.Diagnostics;
using TaskAndDocumentManager.Application.Common.Interfaces;

namespace TaskAndDocumentManager.Api.Middleware;

public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;

    public RequestMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationMetrics metrics)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        metrics.RecordRequest(
            context.Request.Method,
            ResolveRoute(context),
            context.Response.StatusCode,
            stopwatch.Elapsed.TotalMilliseconds);
    }

    private static string ResolveRoute(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        return endpoint?.DisplayName ?? context.Request.Path.Value ?? "unknown";
    }
}
