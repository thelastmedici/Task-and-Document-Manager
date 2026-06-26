using Moq;
using System.Reflection;
using TaskAndDocumentManager.Application.Audit.DTOs;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Audit.UseCases;
using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Infrastructure.Audit;

namespace TaskAndDocumentManager.Application.Tests.Audit.UseCases;

public class ListAuditLogsTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly ListAuditLogs _sut;

    public ListAuditLogsTests()
    {
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _sut = new ListAuditLogs(_auditLogRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPaginatedAuditLogDtos()
    {
        var cancellationToken = CancellationToken.None;
        var workspaceId = Guid.NewGuid();
        var auditLog = new AuditLog(
            Guid.NewGuid(),
            AuditActions.DocumentUploaded,
            nameof(Document),
            Guid.NewGuid(),
            workspaceId);

        _auditLogRepositoryMock
            .Setup(repository => repository.SearchAuditLogsAsync(
                It.Is<AuditQuery>(query =>
                    query.PageNumber == 1 &&
                    query.PageSize == AuditQuery.MaxPageSize &&
                    query.WorkspaceId == workspaceId),
                cancellationToken))
            .ReturnsAsync(new PaginatedResult<AuditLog>(
                new[] { auditLog },
                10,
                1,
                AuditQuery.MaxPageSize));

        var result = await _sut.ExecuteAsync(
            new AuditQuery(PageNumber: 0, PageSize: 1000),
            workspaceId,
            cancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(AuditQuery.MaxPageSize, result.PageSize);
        Assert.Equal(auditLog.Id, item.Id);
        Assert.Equal(workspaceId, item.WorkspaceId);
        Assert.Equal(auditLog.Action, item.Action);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizeAndForwardAuditFilters()
    {
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var timestampFromUtc = new DateTime(2026, 06, 14, 0, 0, 0, DateTimeKind.Utc);
        var timestampToUtc = new DateTime(2026, 06, 14, 23, 59, 59, DateTimeKind.Utc);

        _auditLogRepositoryMock
            .Setup(repository => repository.SearchAuditLogsAsync(
                It.Is<AuditQuery>(query =>
                    query.UserId == userId &&
                    query.Action == AuditActions.DocumentDeleted &&
                    query.TimestampFromUtc == timestampFromUtc &&
                    query.TimestampToUtc == timestampToUtc &&
                    query.WorkspaceId == workspaceId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<AuditLog>(
                Array.Empty<AuditLog>(),
                0,
                1,
                AuditQuery.DefaultPageSize));

        await _sut.ExecuteAsync(new AuditQuery(
            UserId: userId,
            Action: $"  {AuditActions.DocumentDeleted}  ",
            TimestampFromUtc: timestampFromUtc,
            TimestampToUtc: timestampToUtc),
            workspaceId);

        _auditLogRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectUnsupportedAction()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(new AuditQuery(Action: "SomethingRandom"), Guid.NewGuid()));

        Assert.Equal("query", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectInvalidTimestampRange()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(new AuditQuery(
                TimestampFromUtc: new DateTime(2026, 06, 15, 0, 0, 0, DateTimeKind.Utc),
                TimestampToUtc: new DateTime(2026, 06, 14, 0, 0, 0, DateTimeKind.Utc)),
                Guid.NewGuid()));

        Assert.Equal("query", exception.ParamName);
    }

    [Fact]
    public async Task Repository_ShouldFilterByUserActionAndTimestampRange()
    {
        var repository = new AuditLogRepository();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();
        var timestampFromUtc = new DateTime(2026, 06, 14, 0, 0, 0, DateTimeKind.Utc);
        var timestampToUtc = new DateTime(2026, 06, 14, 23, 59, 59, DateTimeKind.Utc);
        var matchingLog = CreateAuditLog(
            userId,
            AuditActions.DocumentDeleted,
            new DateTime(2026, 06, 14, 12, 0, 0, DateTimeKind.Utc),
            workspaceId);
        var wrongActionLog = CreateAuditLog(
            userId,
            AuditActions.DocumentUploaded,
            new DateTime(2026, 06, 14, 13, 0, 0, DateTimeKind.Utc),
            workspaceId);
        var wrongUserLog = CreateAuditLog(
            otherUserId,
            AuditActions.DocumentDeleted,
            new DateTime(2026, 06, 14, 14, 0, 0, DateTimeKind.Utc),
            workspaceId);
        var outsideRangeLog = CreateAuditLog(
            userId,
            AuditActions.DocumentDeleted,
            new DateTime(2026, 06, 13, 12, 0, 0, DateTimeKind.Utc),
            workspaceId);
        var otherWorkspaceLog = CreateAuditLog(
            userId,
            AuditActions.DocumentDeleted,
            new DateTime(2026, 06, 14, 12, 30, 0, DateTimeKind.Utc),
            otherWorkspaceId);

        await repository.AddAsync(matchingLog);
        await repository.AddAsync(wrongActionLog);
        await repository.AddAsync(wrongUserLog);
        await repository.AddAsync(outsideRangeLog);
        await repository.AddAsync(otherWorkspaceLog);

        var result = await repository.SearchAuditLogsAsync(new AuditQuery(
            UserId: userId,
            Action: AuditActions.DocumentDeleted,
            TimestampFromUtc: timestampFromUtc,
            TimestampToUtc: timestampToUtc,
            WorkspaceId: workspaceId));

        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(matchingLog.Id, item.Id);
    }

    [Fact]
    public async Task Repository_ShouldRejectSearchWithoutWorkspaceScope()
    {
        var repository = new AuditLogRepository();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.SearchAuditLogsAsync(new AuditQuery()));

        Assert.Equal("query", exception.ParamName);
    }

    private static AuditLog CreateAuditLog(
        Guid userId,
        string action,
        DateTime timestampUtc,
        Guid workspaceId)
    {
        var auditLog = new AuditLog(userId, action, nameof(Document), Guid.NewGuid(), workspaceId);
        var timestampProperty = typeof(AuditLog).GetProperty(
            nameof(AuditLog.TimestampUtc),
            BindingFlags.Instance | BindingFlags.Public);

        timestampProperty!.SetValue(auditLog, timestampUtc);

        return auditLog;
    }
}
