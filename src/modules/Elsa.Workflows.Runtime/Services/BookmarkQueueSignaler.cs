namespace Elsa.Workflows.Runtime;

public class BookmarkQueueSignaler : IBookmarkQueueSignaler
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