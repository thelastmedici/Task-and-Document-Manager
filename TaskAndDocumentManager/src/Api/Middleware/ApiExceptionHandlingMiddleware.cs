namespace TaskAndDocumentManager.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                ex,
                "Request was cancelled by the client for {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception occurred after the response started for {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
                throw;
            }

            _logger.LogError(
                ex,
                "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(
                new { message = "The request could not be completed. Please try again." },
                context.RequestAborted);
        }
    }
}
