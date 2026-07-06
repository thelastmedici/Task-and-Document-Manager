using Microsoft.AspNetCore.Http;
using TaskAndDocumentManager.Api.Middleware;

namespace TaskAndDocumentManager.Application.Tests.Api.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldAddSecurityHeaders()
    {
        var middleware = new SecurityHeadersMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
        Assert.Contains("frame-ancestors 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
        Assert.Contains("camera=()", context.Response.Headers["Permissions-Policy"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotApplyContentSecurityPolicy_ToSwagger()
    {
        var middleware = new SecurityHeadersMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger/index.html";

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
    }
}
