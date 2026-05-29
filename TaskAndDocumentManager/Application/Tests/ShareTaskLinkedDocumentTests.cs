using Moq;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Documents;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class ShareTaskLinkedDocumentTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly Mock<INotificationDispatcher> _notificationDispatcherMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly ShareTaskLinkedDocument _sut;

    public ShareTaskLinkedDocumentTests()
    {
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _notificationDispatcherMock = new Mock<INotificationDispatcher>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _sut = new ShareTaskLinkedDocument(
            _auditLogRepositoryMock.Object,
            _documentRepositoryMock.Object,
            _documentAccessRepositoryMock.Object,
            _notificationDispatcherMock.Object,
            _notificationRepositoryMock.Object,
            _taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGrantAccess_WhenTargetUserIsTaskParticipant()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var task = new TaskItem("Review report", "Review the linked file", ownerId);
        task.AssignTask(targetUserId);

        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);
        document.LinkToTask(task.Id);

        var request = new ShareTaskLinkedDocumentRequest
        {
            DocumentId = document.Id,
            TaskId = task.Id,
            TargetUserId = targetUserId,
            GrantedByUserId = ownerId
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _taskRepositoryMock
            .Setup(repository => repository.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

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
                    notification.Message == "report.pdf was shared with you for a task you participate in." &&
                    notification.IsRead == false),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationDispatcherMock.Verify(
            dispatcher => dispatcher.DispatchCreatedAsync(
                It.Is<Notification>(notification =>
                    notification.UserId == targetUserId &&
                    notification.Title == "Document shared with you" &&
                    notification.Message == "report.pdf was shared with you for a task you participate in."),
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
    public async Task ExecuteAsync_ShouldThrow_WhenDocumentIsNotLinkedToRequestedTask()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var task = new TaskItem("Review report", "Review the linked file", ownerId);
        var differentTaskId = Guid.NewGuid();

        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);
        document.LinkToTask(task.Id);

        var request = new ShareTaskLinkedDocumentRequest
        {
            DocumentId = document.Id,
            TaskId = differentTaskId,
            TargetUserId = targetUserId,
            GrantedByUserId = ownerId
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(request));

        Assert.Equal("Document is linked to a different task.", exception.Message);
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

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTargetUserIsNotTaskParticipant()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var task = new TaskItem("Review report", "Review the linked file", ownerId);

        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);
        document.LinkToTask(task.Id);

        var request = new ShareTaskLinkedDocumentRequest
        {
            DocumentId = document.Id,
            TaskId = task.Id,
            TargetUserId = targetUserId,
            GrantedByUserId = ownerId
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _taskRepositoryMock
            .Setup(repository => repository.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(request));

        Assert.Equal("Target user must be a participant in the linked task.", exception.Message);
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
