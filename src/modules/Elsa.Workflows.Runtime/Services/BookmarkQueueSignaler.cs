namespace Elsa.Workflows.Runtime;

public class BookmarkQueueSignaler : IBookmarkQueueSignaler
{
    private readonly object _lock = new();
    private TaskCompletionSource<object?> _tcs = new();

    public async Task AwaitAsync(CancellationToken cancellationToken)
    {
        Task waitTask;
        lock (_lock)
        {
            // Capture the current TCS and await it
            waitTask = _tcs.Task;
        }

        await WaitAndResetAsync(waitTask);
    }

    public Task TriggerAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            // If TCS is already in a completed state, no need to set it again.
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.SetResult(null);
            }
        }
        
        return Task.CompletedTask;
    }

    private async Task WaitAndResetAsync(Task waitTask)
    {
        await waitTask;
        lock (_lock)
        {
            // Reset the TCS for the next wait
            _tcs = new TaskCompletionSource<object?>();
        }
    }
}