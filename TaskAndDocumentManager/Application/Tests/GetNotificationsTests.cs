using Moq;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Application.Notifications.UseCases;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Notifications.UseCases;

public class GetNotificationsTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly GetNotifications _sut;

    public GetNotificationsTests()
    {
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _sut = new GetNotifications(_notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnMappedNotifications_ForCurrentUser()
    {
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var notification = new Notification(
            userId,
            workspaceId,
            "Document shared with you",
            "report.pdf was shared with you.");

        _notificationRepositoryMock
            .Setup(repository => repository.GetByUserIdAsync(
                userId,
                workspaceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { notification });

        var result = await _sut.ExecuteAsync(userId, workspaceId, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(notification.Id, item.Id);
        Assert.Equal(notification.Title, item.Title);
        Assert.Equal(notification.Message, item.Message);
        Assert.False(item.IsRead);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectEmptyWorkspaceId()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(Guid.NewGuid(), Guid.Empty, CancellationToken.None));

        Assert.Equal("workspaceId", exception.ParamName);
    }
}
