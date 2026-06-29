using Moq;
using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Notifications.DTOs;
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
        var query = new NotificationQuery(PageNumber: 2, PageSize: 5);

        _notificationRepositoryMock
            .Setup(repository => repository.SearchByUserIdAsync(
                userId,
                workspaceId,
                It.Is<NotificationQuery>(requestedQuery =>
                    requestedQuery.PageNumber == query.PageNumber &&
                    requestedQuery.PageSize == query.PageSize),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Notification>(
                new[] { notification },
                6,
                query.PageNumber,
                query.PageSize));

        var result = await _sut.ExecuteAsync(
            userId,
            workspaceId,
            query,
            CancellationToken.None);

        Assert.Equal(6, result.TotalCount);
        Assert.Equal(query.PageNumber, result.Page);
        Assert.Equal(query.PageSize, result.PageSize);

        var item = Assert.Single(result.Items);
        Assert.Equal(notification.Id, item.Id);
        Assert.Equal(notification.Title, item.Title);
        Assert.Equal(notification.Message, item.Message);
        Assert.False(item.IsRead);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectEmptyWorkspaceId()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(Guid.NewGuid(), Guid.Empty, new NotificationQuery(), CancellationToken.None));

        Assert.Equal("workspaceId", exception.ParamName);
    }
}
