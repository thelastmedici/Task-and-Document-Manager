namespace TaskAndDocumentManager.Api.Realtime;

public sealed class RealtimeDispatchOptions
{
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
