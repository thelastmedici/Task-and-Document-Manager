namespace TaskAndDocumentManager.Application.BackgroundJobs;

public interface IBackgroundJob
{
    string Name { get; }
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
