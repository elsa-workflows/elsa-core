namespace Elsa.Workflows.Runtime;

public class BookmarkQueueWorkerSignaler : IBookmarkQueueWorkerSignaler
{
    private TaskCompletionSource? _tsc;

    public Task AwaitAsync()
    {
        _tsc ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return _tsc.Task;
    }

    public void Trigger()
    {
        _tsc?.TrySetResult();
    }
}