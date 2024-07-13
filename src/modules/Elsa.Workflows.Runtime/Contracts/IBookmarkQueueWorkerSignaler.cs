namespace Elsa.Workflows.Runtime;

public interface IBookmarkQueueWorkerSignaler
{
    Task AwaitAsync();
    void Trigger();
}