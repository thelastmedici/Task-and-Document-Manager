using TaskAndDocumentManager.Application.BackgroundJobs;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tasks.UseCases;

public class SendTaskDeadlineReminders : IBackgroundJob
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly INotificationRepository _notificationRepository;
    private readonly ITaskRepository _taskRepository;

    public SendTaskDeadlineReminders(
        INotificationDispatcher notificationDispatcher,
        INotificationRepository notificationRepository,
        ITaskRepository taskRepository)
    {
        _notificationDispatcher = notificationDispatcher;
        _notificationRepository = notificationRepository;
        _taskRepository = taskRepository;
    }

    public string Name => nameof(SendTaskDeadlineReminders);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reminderCutoff = now.Add(ReminderWindow);
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);

        foreach (var task in tasks.Where(task => ShouldSendReminder(task, now, reminderCutoff)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recipientUserId = task.AssignedToUserId ?? task.OwnerId;
            var notification = new Notification(
                recipientUserId,
                "Task deadline approaching",
                $"Task \"{task.Title}\" is due on {task.DueAtUtc:yyyy-MM-dd HH:mm} UTC.");

            await _notificationRepository.AddAsync(notification, cancellationToken);
            task.MarkDeadlineReminderSent();
            await _taskRepository.UpdateAsync(task, cancellationToken);
            await _notificationDispatcher.DispatchCreatedAsync(notification, cancellationToken);
        }
    }

    private static bool ShouldSendReminder(TaskItem task, DateTime now, DateTime reminderCutoff)
    {
        return !task.IsCompleted &&
            task.DueAtUtc.HasValue &&
            task.DueAtUtc.Value >= now &&
            task.DueAtUtc.Value <= reminderCutoff &&
            !task.DeadlineReminderSentAtUtc.HasValue;
    }
}
