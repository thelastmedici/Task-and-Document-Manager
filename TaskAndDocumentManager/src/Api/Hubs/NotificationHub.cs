using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Api.Realtime;
using TaskAndDocumentManager.Application.Presence.Interfaces;

namespace TaskAndDocumentManager.Api.Hubs;

[Authorize(Policy = AppPolicies.Authenticated)]
public class NotificationHub : Hub
{
    private readonly IUserConnectionTracker _connectionTracker;
    private readonly IHubContext<RealtimeHub> _realtimeHubContext;
    private readonly IPresenceService _presenceService;

    public NotificationHub(
        IUserConnectionTracker connectionTracker,
        IHubContext<RealtimeHub> realtimeHubContext,
        IPresenceService presenceService)
    {
        _connectionTracker = connectionTracker;
        _realtimeHubContext = realtimeHubContext;
        _presenceService = presenceService;
    }

    public override async Task OnConnectedAsync()
    {
        var actorId = Context.User?.GetActorId()
            ?? throw new UnauthorizedAccessException("Authenticated user context is required.");
        var workspaceId = Context.User.GetWorkspaceId();

        await Groups.AddToGroupAsync(Context.ConnectionId, GetWorkspaceUserGroupName(workspaceId, actorId));
        var change = _connectionTracker.AddConnection(actorId, Context.ConnectionId);

        if (change.IsFirstConnection)
        {
            var presence = _presenceService.SetOnline(actorId);
            await _realtimeHubContext.Clients.All.SendAsync(RealtimeEventNames.UserPresenceUpdated, presence);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var actorId = Context.User?.GetActorId();
        var workspaceId = Context.User?.GetWorkspaceId();

        if (actorId.HasValue && workspaceId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                GetWorkspaceUserGroupName(workspaceId.Value, actorId.Value));
            var change = _connectionTracker.RemoveConnection(actorId.Value, Context.ConnectionId);

            if (change.IsLastConnection)
            {
                var presence = _presenceService.SetOffline(actorId.Value);
                await _realtimeHubContext.Clients.All.SendAsync(RealtimeEventNames.UserPresenceUpdated, presence);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string GetWorkspaceUserGroupName(Guid workspaceId, Guid userId)
    {
        return $"workspace:{workspaceId}:user:{userId}";
    }
}
