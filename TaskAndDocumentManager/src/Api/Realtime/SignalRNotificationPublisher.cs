using Microsoft.AspNetCore.SignalR;
using TaskAndDocumentManager.Api.Hubs;
using TaskAndDocumentManager.Application.Notifications.DTOs;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Api.Realtime;

public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(
        IHubContext<NotificationHub> notificationHubContext,
        ILogger<SignalRNotificationPublisher> logger)
    {
        _notificationHubContext = notificationHubContext;
        _logger = logger;
    }

    public async Task PublishCreatedAsync(Notification notification, CancellationToken cancellationToken = default)
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
            await _notificationHubContext.Clients
                .Group(NotificationHub.GetUserGroupName(notification.UserId))
                .SendAsync("NotificationCreated", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Realtime notification delivery failed for notification {NotificationId} and user {UserId}.",
                notification.Id,
                notification.UserId);
        }
    }
}
