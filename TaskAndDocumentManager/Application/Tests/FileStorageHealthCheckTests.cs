using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskAndDocumentManager.Api.Health;

namespace TaskAndDocumentManager.Application.Tests.Api.Health;

public class FileStorageHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenStorageIsWritable()
    {
        var sut = new FileStorageHealthCheck();

        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }
}
