using System.Collections.Concurrent;
using TaskAndDocumentManager.Application.Presence.Interfaces;
using TaskAndDocumentManager.Application.Presence.DTOs;

namespace TaskAndDocumentManager.Api.Realtime;

public class InMemoryPresenceService : IPresenceService
{
    private readonly ConcurrentDictionary<Guid, PresenceDto> _presence = new();

    public PresenceDto SetOnline(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var now = DateTime.UtcNow;
        var dto = _presence.AddOrUpdate(userId,
            _ => new PresenceDto(userId, now, null, true, null),
            (_, existing) => existing with
            {
                ConnectedAtUtc = existing.IsOnline ? existing.ConnectedAtUtc : now,
                DisconnectedAtUtc = null,
                IsOnline = true
            });

        return dto;
    }

    public PresenceDto SetOffline(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var now = DateTime.UtcNow;
        var dto = _presence.AddOrUpdate(userId,
            _ => new PresenceDto(userId, null, now, false, null),
            (_, existing) => existing with
            {
                DisconnectedAtUtc = now,
                IsOnline = false,
                CurrentDocumentId = null
            });

        return dto;
    }

    public PresenceDto? SetEditing(Guid userId, Guid? documentId, bool isEditing)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (isEditing && (!documentId.HasValue || documentId.Value == Guid.Empty))
        {
            throw new ArgumentException("Document ID is required when editing starts.", nameof(documentId));
        }

        if (!_presence.TryGetValue(userId, out var existing))
        {
            return null;
        }

        if (!existing.IsOnline)
        {
            return null;
        }

        var updated = existing with { CurrentDocumentId = isEditing ? documentId : null };
        _presence[userId] = updated;
        return updated;
    }

    public PresenceDto? GetPresence(Guid userId)
    {
        return _presence.TryGetValue(userId, out var dto) ? dto : null;
    }

    public IReadOnlyCollection<PresenceDto> GetOnlineUsers()
    {
        return _presence.Values
            .Where(presence => presence.IsOnline)
            .OrderBy(presence => presence.ConnectedAtUtc)
            .ToList();
    }

    public IReadOnlyCollection<PresenceDto> GetActiveCollaborators(Guid documentId)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document ID is required.", nameof(documentId));
        }

        return _presence.Values
            .Where(presence =>
                presence.IsOnline &&
                presence.CurrentDocumentId == documentId)
            .OrderBy(presence => presence.ConnectedAtUtc)
            .ToList();
    }
}
