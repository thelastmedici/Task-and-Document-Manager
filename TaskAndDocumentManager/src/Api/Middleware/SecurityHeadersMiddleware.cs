namespace TaskAndDocumentManager.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "connect-src 'self' ws: wss:; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "img-src 'self' data:; " +
        "object-src 'none'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";

        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
        }

        await _next(context);
    }
}
