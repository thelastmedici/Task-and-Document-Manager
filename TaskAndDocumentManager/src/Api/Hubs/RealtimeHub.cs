using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Api.Realtime;
using TaskAndDocumentManager.Application.Presence.Interfaces;
using TaskAndDocumentManager.Application.Presence.DTOs;

namespace TaskAndDocumentManager.Api.Hubs;

[Authorize(Policy = AppPolicies.Authenticated)]
public class RealtimeHub : Hub
{
    private readonly IUserConnectionTracker _connectionTracker;
    private readonly IPresenceService _presenceService;

    public RealtimeHub(IUserConnectionTracker connectionTracker, IPresenceService presenceService)
    {
        _connectionTracker = connectionTracker;
        _presenceService = presenceService;
    }

    public override async Task OnConnectedAsync()
    {
        var actorId = Context.User?.GetActorId()
            ?? throw new UnauthorizedAccessException("Authenticated user context is required.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(actorId));
        var change = _connectionTracker.AddConnection(actorId, Context.ConnectionId);

        if (change.IsFirstConnection)
        {
            var presence = _presenceService.SetOnline(actorId);
            await Clients.AllExcept(Context.ConnectionId).SendAsync(RealtimeEventNames.UserPresenceUpdated, presence);
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
                var presence = _presenceService.SetOffline(actorId.Value);
                await Clients.All.SendAsync(RealtimeEventNames.UserPresenceUpdated, presence);
            }
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

    public async Task StartEditingDocument(Guid documentId)
    {
        var actorId = Context.User?.GetActorId()
            ?? throw new UnauthorizedAccessException("Authenticated user context is required.");

        if (documentId == Guid.Empty)
        {
            throw new HubException("Document ID is required.");
        }

        var presence = _presenceService.SetEditing(actorId, documentId, true);
        if (presence is not null)
        {
            await Clients.Group(GetUserGroupName(actorId)).SendAsync(RealtimeEventNames.UserPresenceUpdated, presence);
        }
    }

    public async Task StopEditingDocument(Guid documentId)
    {
        var actorId = Context.User?.GetActorId()
            ?? throw new UnauthorizedAccessException("Authenticated user context is required.");

        if (documentId == Guid.Empty)
        {
            throw new HubException("Document ID is required.");
        }

        var presence = _presenceService.SetEditing(actorId, null, false);
        if (presence is not null)
        {
            await Clients.Group(GetUserGroupName(actorId)).SendAsync(RealtimeEventNames.UserPresenceUpdated, presence);
        }
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
