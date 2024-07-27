namespace Elsa.Workflows.Runtime;

public interface IBookmarkQueueSignaler
{
    Task AwaitAsync();
    void Trigger();
}