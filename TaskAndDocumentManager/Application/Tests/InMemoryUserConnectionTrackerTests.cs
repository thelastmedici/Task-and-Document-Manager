using TaskAndDocumentManager.Api.Realtime;

namespace TaskAndDocumentManager.Application.Tests.Realtime;

public class InMemoryUserConnectionTrackerTests
{
    [Fact]
    public void AddConnection_ShouldTrackMultipleConnections_ForSameUser()
    {
        var tracker = new InMemoryUserConnectionTracker();
        var userId = Guid.NewGuid();

        var first = tracker.AddConnection(userId, "conn-1");
        var second = tracker.AddConnection(userId, "conn-2");

        Assert.True(first.IsFirstConnection);
        Assert.False(second.IsFirstConnection);
        Assert.Equal(2, tracker.GetConnections(userId).Count);
        Assert.True(tracker.IsOnline(userId));
    }

    [Fact]
    public void RemoveConnection_ShouldKeepUserOnline_UntilLastConnectionCloses()
    {
        var tracker = new InMemoryUserConnectionTracker();
        var userId = Guid.NewGuid();

        tracker.AddConnection(userId, "conn-1");
        tracker.AddConnection(userId, "conn-2");

        var firstRemoval = tracker.RemoveConnection(userId, "conn-1");
        var secondRemoval = tracker.RemoveConnection(userId, "conn-2");

        Assert.False(firstRemoval.IsLastConnection);
        Assert.True(secondRemoval.IsLastConnection);
        Assert.False(tracker.IsOnline(userId));
    }

    [Fact]
    public void RemoveConnection_ShouldReturnEmptyState_WhenUserWasNotTracked()
    {
        var tracker = new InMemoryUserConnectionTracker();

        var change = tracker.RemoveConnection(Guid.NewGuid(), "unknown-connection");

        Assert.True(change.IsLastConnection);
    }
}
