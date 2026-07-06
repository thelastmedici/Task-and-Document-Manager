using Microsoft.AspNetCore.RateLimiting;
using TaskAndDocumentManager.Api.Security;
using TaskAndDocumentManager.Controllers;

namespace TaskAndDocumentManager.Application.Tests.Api.Security;

public class RateLimitPolicyAttributeTests
{
    [Fact]
    public void Login_ShouldUseAuthSensitiveRateLimitPolicy()
    {
        var attribute = GetRateLimitAttribute<AuthController>(nameof(AuthController.Login));

        Assert.Equal(RateLimitPolicies.AuthSensitive, attribute.PolicyName);
    }

    [Fact]
    public void Register_ShouldUseAuthSensitiveRateLimitPolicy()
    {
        var attribute = GetRateLimitAttribute<AuthController>(nameof(AuthController.Register));

        Assert.Equal(RateLimitPolicies.AuthSensitive, attribute.PolicyName);
    }

    [Fact]
    public void Upload_ShouldUseFileUploadRateLimitPolicy()
    {
        var attribute = GetRateLimitAttribute<DocumentsController>(nameof(DocumentsController.Upload));

        Assert.Equal(RateLimitPolicies.FileUpload, attribute.PolicyName);
    }

    private static EnableRateLimitingAttribute GetRateLimitAttribute<TController>(string actionName)
    {
        var method = typeof(TController)
            .GetMethods()
            .Single(method => method.Name == actionName);

        return method
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Cast<EnableRateLimitingAttribute>()
            .Single();
    }
}
