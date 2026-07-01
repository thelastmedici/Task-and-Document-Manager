using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TaskAndDocumentManager.Api.Hubs;
using TaskAndDocumentManager.Application.Notifications.DTOs;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Api.Realtime;

public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly ILogger<SignalRNotificationDispatcher> _logger;
    private readonly RealtimeDispatchOptions _options;

    public SignalRNotificationDispatcher(
        IHubContext<NotificationHub> notificationHubContext,
        ILogger<SignalRNotificationDispatcher> logger,
        IOptions<RealtimeDispatchOptions> options)
    {
        _notificationHubContext = notificationHubContext;
        _logger = logger;
        _options = options.Value;
    }

    public async Task DispatchCreatedAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var payload = new NotificationDto(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAtUtc);

        try
        {
            using var timeoutSource = CreateTimeoutSource(cancellationToken, out var operationToken);

            await _notificationHubContext.Clients
                .Group(NotificationHub.GetWorkspaceUserGroupName(notification.WorkspaceId, notification.UserId))
                .SendAsync(RealtimeEventNames.NotificationCreated, payload, operationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Realtime notification delivery timed out for notification {NotificationId}, workspace {WorkspaceId}, and user {UserId}.",
                notification.Id,
                notification.WorkspaceId,
                notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Realtime notification delivery failed for notification {NotificationId}, workspace {WorkspaceId}, and user {UserId}.",
                notification.Id,
                notification.WorkspaceId,
                notification.UserId);
        }
    }

    private CancellationTokenSource? CreateTimeoutSource(
        CancellationToken cancellationToken,
        out CancellationToken operationToken)
    {
        if (_options.OperationTimeout <= TimeSpan.Zero)
        {
            operationToken = cancellationToken;
            return null;
        }

        var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_options.OperationTimeout);
        operationToken = timeoutSource.Token;
        return timeoutSource;
    }
}
