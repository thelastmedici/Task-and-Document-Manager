using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;

namespace TaskAndDocumentManager.Api.Hubs;

[Authorize(Policy = AppPolicies.Authenticated)]
public class RealtimeHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var actorId = Context.User?.GetActorId()
            ?? throw new UnauthorizedAccessException("Authenticated user context is required.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(actorId));
        await Clients.Others.SendAsync("UserOnline", actorId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var actorId = Context.User?.GetActorId();

        if (actorId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(actorId.Value));
            await Clients.Others.SendAsync("UserOffline", actorId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public Task JoinTask(Guid taskId)
    {
        if (taskId == Guid.Empty)
        {
            throw new HubException("Task ID is required.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, GetTaskGroupName(taskId));
    }

    public Task LeaveTask(Guid taskId)
    {
        if (taskId == Guid.Empty)
        {
            throw new HubException("Task ID is required.");
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetTaskGroupName(taskId));
    }

    public static string GetUserGroupName(Guid userId)
    {
        return $"user:{userId}";
    }

    public static string GetTaskGroupName(Guid taskId)
    {
        return $"task:{taskId}";
    }
}
