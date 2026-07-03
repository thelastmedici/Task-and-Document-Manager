using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskAndDocumentManager.Api.Health;

public sealed class FileStorageHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var storageRoot = Path.Combine(AppContext.BaseDirectory, "storage", "uploads");
        var probePath = Path.Combine(storageRoot, $".health-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(storageRoot);
            File.WriteAllText(probePath, "ok");
            File.Delete(probePath);

            return Task.FromResult(HealthCheckResult.Healthy("File storage is writable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("File storage health check failed.", ex));
        }
    }
}
