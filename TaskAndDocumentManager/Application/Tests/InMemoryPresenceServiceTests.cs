using TaskAndDocumentManager.Api.Realtime;

namespace TaskAndDocumentManager.Application.Tests.Realtime;

public class InMemoryPresenceServiceTests
{
    [Fact]
    public void SetOnline_ShouldTrackConnectedTimestamp()
    {
        var service = new InMemoryPresenceService();
        var userId = Guid.NewGuid();

        var presence = service.SetOnline(userId);

        Assert.Equal(userId, presence.UserId);
        Assert.True(presence.IsOnline);
        Assert.NotNull(presence.ConnectedAtUtc);
        Assert.Null(presence.DisconnectedAtUtc);
        Assert.Null(presence.CurrentDocumentId);
    }

    [Fact]
    public void SetOffline_ShouldTrackDisconnectedTimestampAndClearEditingState()
    {
        var service = new InMemoryPresenceService();
        var userId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        service.SetOnline(userId);
        service.SetEditing(userId, documentId, isEditing: true);

        var presence = service.SetOffline(userId);

        Assert.False(presence.IsOnline);
        Assert.NotNull(presence.DisconnectedAtUtc);
        Assert.Null(presence.CurrentDocumentId);
        Assert.False(presence.IsEditing);
    }

    [Fact]
    public void SetEditing_ShouldTrackActiveCollaborator_WhenUserIsOnline()
    {
        var service = new InMemoryPresenceService();
        var userId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        service.SetOnline(userId);
        var presence = service.SetEditing(userId, documentId, isEditing: true);
        var collaborators = service.GetActiveCollaborators(documentId);

        Assert.NotNull(presence);
        Assert.Equal(documentId, presence.CurrentDocumentId);
        Assert.True(presence.IsEditing);
        Assert.Single(collaborators);
    }

    [Fact]
    public void SetEditing_ShouldReturnNull_WhenUserIsOffline()
    {
        var service = new InMemoryPresenceService();

        var presence = service.SetEditing(Guid.NewGuid(), Guid.NewGuid(), isEditing: true);

        Assert.Null(presence);
    }

    [Fact]
    public void GetOnlineUsers_ShouldReturnOnlyOnlineUsers()
    {
        var service = new InMemoryPresenceService();
        var onlineUserId = Guid.NewGuid();
        var offlineUserId = Guid.NewGuid();

        service.SetOnline(onlineUserId);
        service.SetOnline(offlineUserId);
        service.SetOffline(offlineUserId);

        var onlineUsers = service.GetOnlineUsers();

        var presence = Assert.Single(onlineUsers);
        Assert.Equal(onlineUserId, presence.UserId);
    }
}
