using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAndDocumentManager.Api.Middleware;

namespace TaskAndDocumentManager.Application.Tests.Api.Middleware;

public class ApiExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturnGenericFailureMessage_WhenUnhandledExceptionOccurs()
    {
        var middleware = new ApiExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Object reference not set to an instance of an object."),
            NullLogger<ApiExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();

        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await middleware.InvokeAsync(context);

        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var response = await reader.ReadToEndAsync();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains("The request could not be completed. Please try again.", response);
        Assert.DoesNotContain("Object reference", response);
    }
}
