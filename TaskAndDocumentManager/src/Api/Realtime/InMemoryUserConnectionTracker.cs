using System.Collections.Concurrent;

namespace TaskAndDocumentManager.Api.Realtime;

public class InMemoryUserConnectionTracker : IUserConnectionTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connections = new();

    public ConnectionChange AddConnection(Guid userId, string connectionId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("Connection ID is required.", nameof(connectionId));
        }

        var userConnections = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        userConnections.TryAdd(connectionId, 0);

        return new ConnectionChange(userConnections.Count);
    }

    public ConnectionChange RemoveConnection(Guid userId, string connectionId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("Connection ID is required.", nameof(connectionId));
        }

        if (!_connections.TryGetValue(userId, out var userConnections))
        {
            return new ConnectionChange(0);
        }

        userConnections.TryRemove(connectionId, out _);
        var activeConnections = userConnections.Count;

        if (activeConnections == 0)
        {
            _connections.TryRemove(userId, out _);
        }

        return new ConnectionChange(activeConnections);
    }

    public IReadOnlyCollection<string> GetConnections(Guid userId)
    {
        if (!_connections.TryGetValue(userId, out var userConnections))
        {
            return Array.Empty<string>();
        }

        return userConnections.Keys.ToList();
    }

    public bool IsOnline(Guid userId)
    {
        return _connections.TryGetValue(userId, out var userConnections) && userConnections.Count > 0;
    }
}
