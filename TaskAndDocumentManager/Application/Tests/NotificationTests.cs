using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Domain;

public class NotificationTests
{
    [Fact]
    public void Constructor_ShouldCreateUnreadNotification_WhenInputIsValid()
    {
        var userId = Guid.NewGuid();

        var notification = new Notification(
            userId,
            Guid.NewGuid(),
            "Document shared with you",
            "Opeyemi shared resume.pdf with you.");

        Assert.Equal(userId, notification.UserId);
        Assert.Equal("Document shared with you", notification.Title);
        Assert.Equal("Opeyemi shared resume.pdf with you.", notification.Message);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public void MarkAsRead_ShouldSetIsReadToTrue()
    {
        var notification = new Notification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Task assigned",
            "A task was assigned to you.");

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
    }
}
