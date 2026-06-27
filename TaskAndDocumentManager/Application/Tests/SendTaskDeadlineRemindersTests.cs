using Moq;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tests.Tasks.UseCases;

public class SendTaskDeadlineRemindersTests
{
    private readonly Mock<INotificationDispatcher> _notificationDispatcherMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly SendTaskDeadlineReminders _sut;

    public SendTaskDeadlineRemindersTests()
    {
        _notificationDispatcherMock = new Mock<INotificationDispatcher>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _sut = new SendTaskDeadlineReminders(
            _notificationDispatcherMock.Object,
            _notificationRepositoryMock.Object,
            _taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateNotificationAndMarkReminderSent_ForTaskDueWithinNextDay()
    {
        var ownerId = Guid.NewGuid();
        var assignedUserId = Guid.NewGuid();
        var task = new TaskItem(
            "Finish report",
            "Complete the final report",
            ownerId,
            DateTime.UtcNow.AddHours(12));
        task.AssignTask(assignedUserId);

        _taskRepositoryMock
            .Setup(repository => repository.GetAllForMaintenanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { task });

        await _sut.ExecuteAsync(CancellationToken.None);

        _notificationRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Notification>(notification =>
                    notification.UserId == assignedUserId &&
                    notification.Title == "Task deadline approaching" &&
                    notification.Message.Contains("Finish report")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _taskRepositoryMock.Verify(
            repository => repository.UpdateAsync(
                It.Is<TaskItem>(updatedTask =>
                    updatedTask.Id == task.Id &&
                    updatedTask.DeadlineReminderSentAtUtc.HasValue),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _notificationDispatcherMock.Verify(
            dispatcher => dispatcher.DispatchCreatedAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCreateDuplicateNotification_WhenReminderWasAlreadySent()
    {
        var task = new TaskItem(
            "Finish report",
            "Complete the final report",
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(12));
        task.MarkDeadlineReminderSent();

        _taskRepositoryMock
            .Setup(repository => repository.GetAllForMaintenanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { task });

        await _sut.ExecuteAsync(CancellationToken.None);

        _notificationRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _taskRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipCompletedTasks()
    {
        var task = new TaskItem(
            "Finish report",
            "Complete the final report",
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(12));
        task.MarkCompleted();

        _taskRepositoryMock
            .Setup(repository => repository.GetAllForMaintenanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { task });

        await _sut.ExecuteAsync(CancellationToken.None);

        _notificationRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
