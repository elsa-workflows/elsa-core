using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Workflows.Designer.Services;

/// <summary>
/// A queue for pending actions that can be processed in a FIFO manner.
/// </summary>
public class PendingActionsQueue(Func<ValueTask<bool>> shortCircuit, Func<ILogger> logger)
{
    private readonly Queue<Func<Task>> _pendingActions = new();

    /// <summary>
    /// Processes all pending actions.
    /// </summary>
    public async Task ProcessAsync()
    {
        while (_pendingActions.Any())
        {
            var action = _pendingActions.Dequeue();
            await TryExecuteActionAsync(action);
        }
    }

    /// <summary>
    /// Enqueues an action to be executed.
    /// </summary>
    public async Task EnqueueAsync(Func<Task> action)
    {
        if(await shortCircuit())
        {
            await TryExecuteActionAsync(action);
            return;
        }

        var tsc = new TaskCompletionSource();
        _pendingActions.Enqueue(async () =>
        {
            await action();
            tsc.SetResult();
        });
        await tsc.Task;
    }

    /// <summary>
    /// Enqueues an action to be executed.
    /// </summary>
    public async Task<T> EnqueueAsync<T>(Func<Task<T>> action)
    {
        if(await shortCircuit())
            return await action();
        
        var tsc = new TaskCompletionSource<T>();
        _pendingActions.Enqueue(async () =>
        {
            var result = await action();
            tsc.SetResult(result);
        });
        return await tsc.Task;
    }

    private async Task TryExecuteActionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch(Exception ex)
        {
            logger().LogWarning(ex, "An error occurred while executing a pending action.");
        }
    }
}