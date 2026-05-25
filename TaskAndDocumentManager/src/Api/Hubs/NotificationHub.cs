using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Api.Realtime;

namespace TaskAndDocumentManager.Api.Hubs;

[Authorize(Policy = AppPolicies.Authenticated)]
public class NotificationHub : Hub
{
    private readonly IUserConnectionTracker _connectionTracker;
    private readonly IHubContext<RealtimeHub> _realtimeHubContext;

    public NotificationHub(
        IUserConnectionTracker connectionTracker,
        IHubContext<RealtimeHub> realtimeHubContext)
    {
        _connectionTracker = connectionTracker;
        _realtimeHubContext = realtimeHubContext;
    }

    public override async Task OnConnectedAsync()
    {
        var actorId = Context.User?.GetActorId()
            ?? throw new UnauthorizedAccessException("Authenticated user context is required.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(actorId));
        var change = _connectionTracker.AddConnection(actorId, Context.ConnectionId);

        if (change.IsFirstConnection)
        {
            await _realtimeHubContext.Clients.All.SendAsync("UserOnline", actorId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var actorId = Context.User?.GetActorId();

        if (actorId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(actorId.Value));
            var change = _connectionTracker.RemoveConnection(actorId.Value, Context.ConnectionId);

            if (change.IsLastConnection)
            {
                await _realtimeHubContext.Clients.All.SendAsync("UserOffline", actorId.Value);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string GetUserGroupName(Guid userId)
    {
        return $"user:{userId}";
    }
}
