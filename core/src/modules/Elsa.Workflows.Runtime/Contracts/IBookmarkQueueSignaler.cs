namespace Elsa.Workflows.Runtime;

public interface IBookmarkQueueSignaler
{
    Task AwaitAsync(CancellationToken cancellationToken = default);
    Task TriggerAsync(CancellationToken cancellationToken = default);
}