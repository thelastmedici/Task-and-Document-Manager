namespace TaskAndDocumentManager.Api.BackgroundJobs;

public class BackgroundJobOptions
{
    public bool Enabled { get; set; } = true;
    public bool RunOnStartup { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}
