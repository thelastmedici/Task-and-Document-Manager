using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Domain;

public class AuditLogTests
{
    [Fact]
    public void Constructor_ShouldCreateAuditLog_WhenActionIsSupported()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var auditLog = new AuditLog(
            userId,
            AuditActions.DocumentUploaded,
            "Document",
            entityId);

        Assert.Equal(userId, auditLog.UserId);
        Assert.Equal(AuditActions.DocumentUploaded, auditLog.Action);
        Assert.Equal("Document", auditLog.EntityType);
        Assert.Equal(entityId, auditLog.EntityId);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenActionIsNotSupported()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new AuditLog(
                Guid.NewGuid(),
                "user did something",
                "Document",
                Guid.NewGuid()));

        Assert.Equal("action", exception.ParamName);
    }
}
