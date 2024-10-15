using System.Collections.Concurrent;

namespace Elsa.Testing.Shared.Services;

public class SignalManager
{
    private readonly ConcurrentDictionary<object, TaskCompletionSource<object?>> _signals = new();

    public async Task<T> WaitAsync<T>(object signal, int millisecondsTimeout = 8000)
    {
        return await WaitAsync(signal, millisecondsTimeout) is T result ? result : throw new InvalidCastException($"Signal '{signal}' was not of type '{typeof(T).Name}'.");
    }

    public async Task<object?> WaitAsync(object signal, int millisecondsTimeout = 8000)
    {
        var taskCompletionSource = GetOrCreate(signal);
        using var cancellationTokenSource = new CancellationTokenSource(millisecondsTimeout);
        try
        {
            await Task.WhenAny(taskCompletionSource.Task, Task.Delay(millisecondsTimeout, cancellationTokenSource.Token));
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
            return taskCompletionSource.Task.Result;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Signal '{signal}' timed out after {millisecondsTimeout} milliseconds.");
        }
    }

    public void Trigger(object signal, object? result = null)
    {
        var taskCompletionSource = GetOrCreate(signal);
    
        if (taskCompletionSource.Task.IsCompleted)
            return;
        
        taskCompletionSource.SetResult(result);
    }

    private TaskCompletionSource<object?> GetOrCreate(object eventName)
    {
        return _signals.GetOrAdd(eventName, _ => new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously));
    }
}