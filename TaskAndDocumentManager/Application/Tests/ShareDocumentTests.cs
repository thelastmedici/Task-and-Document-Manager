using Moq;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Domain.Documents;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class ShareDocumentTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly Mock<INotificationDispatcher> _notificationDispatcherMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly ShareDocument _sut;

    public ShareDocumentTests()
    {
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _notificationDispatcherMock = new Mock<INotificationDispatcher>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _sut = new ShareDocument(
            _auditLogRepositoryMock.Object,
            _documentRepositoryMock.Object,
            _documentAccessRepositoryMock.Object,
            _notificationDispatcherMock.Object,
            _notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGrantAccess_WhenOwnerSharesDocument()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);
        var request = new ShareDocumentRequest
        {
            DocumentId = document.Id,
            TargetUserId = targetUserId,
            GrantedByUserId = ownerId
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.GrantAccessAsync(It.IsAny<DocumentAccess>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ExecuteAsync(request);

        _documentAccessRepositoryMock.Verify(repository =>
            repository.GrantAccessAsync(
                It.Is<DocumentAccess>(access =>
                    access.DocumentId == document.Id &&
                    access.UserId == targetUserId &&
                    access.GrantedByUserId == ownerId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Notification>(notification =>
                    notification.UserId == targetUserId &&
                    notification.Title == "Document shared with you" &&
                    notification.Message == "report.pdf was shared with you." &&
                    notification.IsRead == false),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationDispatcherMock.Verify(
            dispatcher => dispatcher.DispatchCreatedAsync(
                It.Is<Notification>(notification =>
                    notification.UserId == targetUserId &&
                    notification.Title == "Document shared with you" &&
                    notification.Message == "report.pdf was shared with you."),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(auditLog =>
                    auditLog.UserId == ownerId &&
                    auditLog.Action == AuditActions.DocumentShared &&
                    auditLog.EntityType == nameof(Document) &&
                    auditLog.EntityId == document.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenOwnerSharesWithSelf()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);
        var request = new ShareDocumentRequest
        {
            DocumentId = document.Id,
            TargetUserId = ownerId,
            GrantedByUserId = ownerId
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(request));

        Assert.Equal("You cannot share a document with yourself.", exception.Message);
        _notificationDispatcherMock.Verify(
            dispatcher => dispatcher.DispatchCreatedAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
