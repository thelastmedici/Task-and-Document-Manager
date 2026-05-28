using System.Collections.Concurrent;
using TaskAndDocumentManager.Application.Presence.Interfaces;
using TaskAndDocumentManager.Application.Presence.DTOs;

namespace TaskAndDocumentManager.Api.Realtime;

public class InMemoryPresenceService : IPresenceService
{
    private readonly ConcurrentDictionary<Guid, PresenceDto> _presence = new();

    public PresenceDto SetOnline(Guid userId)
    {
        var dto = _presence.AddOrUpdate(userId,
            _ => new PresenceDto(userId, DateTime.UtcNow, null, true, null),
            (_, existing) => existing with { ConnectedAtUtc = DateTime.UtcNow, DisconnectedAtUtc = null, IsOnline = true });

        return dto;
    }

    public PresenceDto SetOffline(Guid userId)
    {
        var dto = _presence.AddOrUpdate(userId,
            _ => new PresenceDto(userId, DateTime.UtcNow, DateTime.UtcNow, false, null),
            (_, existing) => existing with { DisconnectedAtUtc = DateTime.UtcNow, IsOnline = false });

        return dto;
    }

    public PresenceDto? SetEditing(Guid userId, Guid? documentId, bool isEditing)
    {
        if (!_presence.TryGetValue(userId, out var existing))
        {
            existing = new PresenceDto(userId, isEditing ? DateTime.UtcNow : (DateTime?)null, null, true, isEditing ? documentId : null);
            _presence[userId] = existing;
            return existing;
        }

        var updated = existing with { CurrentDocumentId = isEditing ? documentId : null };
        _presence[userId] = updated;
        return updated;
    }

    public PresenceDto? GetPresence(Guid userId)
    {
        return _presence.TryGetValue(userId, out var dto) ? dto : null;
    }
}
