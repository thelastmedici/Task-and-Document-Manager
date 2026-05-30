using Microsoft.Extensions.Options;
using TaskAndDocumentManager.Application.BackgroundJobs;

namespace TaskAndDocumentManager.Api.BackgroundJobs;

public class ScheduledBackgroundJobService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledBackgroundJobService> _logger;
    private readonly BackgroundJobOptions _options;

    public ScheduledBackgroundJobService(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundJobOptions> options,
        ILogger<ScheduledBackgroundJobService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Scheduled background jobs are disabled.");
            return;
        }

        if (_options.RunOnStartup)
        {
            await RunJobsAsync(stoppingToken);
        }
        else if (_options.InitialDelay > TimeSpan.Zero)
        {
            await Task.Delay(_options.InitialDelay, stoppingToken);
        }

        using var timer = new PeriodicTimer(_options.Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunJobsAsync(stoppingToken);
        }
    }

    private async Task RunJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider.GetServices<IBackgroundJob>();

        foreach (var job in jobs)
        {
            try
            {
                _logger.LogInformation("Starting background job {JobName}.", job.Name);
                await job.ExecuteAsync(stoppingToken);
                _logger.LogInformation("Finished background job {JobName}.", job.Name);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job {JobName} failed.", job.Name);
            }
        }
    }
}
