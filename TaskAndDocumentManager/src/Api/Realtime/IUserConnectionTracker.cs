namespace TaskAndDocumentManager.Api.Realtime;

public interface IUserConnectionTracker
{
    ConnectionChange AddConnection(Guid userId, string connectionId);
    ConnectionChange RemoveConnection(Guid userId, string connectionId);
    IReadOnlyCollection<string> GetConnections(Guid userId);
    bool IsOnline(Guid userId);
}

public sealed record ConnectionChange(int ActiveConnections)
{
    public bool IsFirstConnection => ActiveConnections == 1;
    public bool IsLastConnection => ActiveConnections == 0;
}
