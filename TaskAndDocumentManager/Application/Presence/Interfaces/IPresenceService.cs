using TaskAndDocumentManager.Application.Presence.DTOs;

namespace TaskAndDocumentManager.Application.Presence.Interfaces;

public interface IPresenceService
{
    PresenceDto SetOnline(Guid userId);
    PresenceDto SetOffline(Guid userId);
    PresenceDto? SetEditing(Guid userId, Guid? documentId, bool isEditing);
    PresenceDto? GetPresence(Guid userId);
}
