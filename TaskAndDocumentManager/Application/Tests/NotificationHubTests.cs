using TaskAndDocumentManager.Api.Hubs;

namespace TaskAndDocumentManager.Application.Tests.Api.Hubs;

public class NotificationHubTests
{
    [Fact]
    public void GetWorkspaceUserGroupName_ShouldScopeGroupToWorkspaceAndUser()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var groupName = NotificationHub.GetWorkspaceUserGroupName(workspaceId, userId);

        Assert.Equal($"workspace:{workspaceId}:user:{userId}", groupName);
    }
}
