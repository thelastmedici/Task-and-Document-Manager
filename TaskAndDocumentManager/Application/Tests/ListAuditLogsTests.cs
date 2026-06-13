using Moq;
using TaskAndDocumentManager.Application.Audit.DTOs;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Audit.UseCases;
using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Domain.Entities;

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
        var auditLog = new AuditLog(
            Guid.NewGuid(),
            AuditActions.DocumentUploaded,
            nameof(Document),
            Guid.NewGuid());

        _auditLogRepositoryMock
            .Setup(repository => repository.GetPageAsync(
                It.Is<AuditLogQuery>(query =>
                    query.PageNumber == 1 &&
                    query.PageSize == AuditLogQuery.MaxPageSize),
                cancellationToken))
            .ReturnsAsync(new PaginatedResult<AuditLog>(
                new[] { auditLog },
                10,
                1,
                AuditLogQuery.MaxPageSize));

        var result = await _sut.ExecuteAsync(
            new AuditLogQuery(PageNumber: 0, PageSize: 1000),
            cancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(AuditLogQuery.MaxPageSize, result.PageSize);
        Assert.Equal(auditLog.Id, item.Id);
        Assert.Equal(auditLog.Action, item.Action);
    }
}
