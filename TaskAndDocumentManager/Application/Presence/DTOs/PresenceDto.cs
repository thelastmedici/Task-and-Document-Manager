namespace TaskAndDocumentManager.Application.Presence.DTOs;

public sealed record PresenceDto(
    Guid UserId,
    DateTime? ConnectedAtUtc,
    DateTime? DisconnectedAtUtc,
    bool IsOnline,
    Guid? CurrentDocumentId)
{
    public bool IsEditing => CurrentDocumentId.HasValue && IsOnline;
}
