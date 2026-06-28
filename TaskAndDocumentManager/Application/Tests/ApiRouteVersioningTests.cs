using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Api.Routing;
using TaskAndDocumentManager.Controllers;

namespace TaskAndDocumentManager.Application.Tests.Api.Routing;

public class ApiRouteVersioningTests
{
    [Theory]
    [InlineData(typeof(AuditLogsController), ApiRoutes.AuditLogs)]
    [InlineData(typeof(AuthController), ApiRoutes.Auth)]
    [InlineData(typeof(DocumentsController), ApiRoutes.Documents)]
    [InlineData(typeof(NotificationsController), ApiRoutes.Notifications)]
    [InlineData(typeof(SearchController), ApiRoutes.Search)]
    [InlineData(typeof(TaskController), ApiRoutes.Tasks)]
    [InlineData(typeof(TeamsController), ApiRoutes.Teams)]
    public void ApiControllers_ShouldUseV1Routes(Type controllerType, string expectedRoute)
    {
        var route = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .OfType<RouteAttribute>()
            .Single();

        Assert.Equal(expectedRoute, route.Template);
        Assert.StartsWith(ApiRoutes.V1, route.Template);
    }
}
