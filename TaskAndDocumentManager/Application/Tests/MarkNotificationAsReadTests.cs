using Moq;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Application.Notifications.UseCases;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Notifications.UseCases;

public class MarkNotificationAsReadTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly MarkNotificationAsRead _sut;

    public MarkNotificationAsReadTests()
    {
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _sut = new MarkNotificationAsRead(_notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkNotificationAsRead_WhenOwnedByUser()
    {
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var notification = new Notification(userId, workspaceId, "Task assigned to you", "Review the report.");

        _notificationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                notification.Id,
                workspaceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        await _sut.ExecuteAsync(notification.Id, userId, workspaceId, CancellationToken.None);

        Assert.True(notification.IsRead);
        _notificationRepositoryMock.Verify(
            repository => repository.UpdateAsync(notification, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenNotificationBelongsToDifferentUser()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var notification = new Notification(ownerId, workspaceId, "Task assigned to you", "Review the report.");

        _notificationRepositoryMock
            .Setup(repository => repository.GetByIdAsync(
                notification.Id,
                workspaceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(notification.Id, requesterId, workspaceId, CancellationToken.None));

        Assert.Equal("You can only update your own notifications.", exception.Message);
        _notificationRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectEmptyWorkspaceId()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, CancellationToken.None));

        Assert.Equal("workspaceId", exception.ParamName);
    }
}
