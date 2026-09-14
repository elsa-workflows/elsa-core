using Elsa.Workflows.ComponentTests.Services;

namespace Elsa.Workflows.ComponentTests.Fixtures;

/// <summary>
/// Lazily creates independent in-memory hosts that share only this invocation's catalog and lock directory.
/// </summary>
public sealed class Cluster : IAsyncDisposable
{
    private readonly Lock _lifecycleLock = new();
    private readonly WorkflowServer _pod1;
    private readonly Lazy<Task<WorkflowServer>> _pod2;
    private readonly Lazy<Task<WorkflowServer>> _pod3;
    private int _disposed;

    public Cluster(WorkflowServer pod1, Func<Task<WorkflowServer>> createAdditionalPod)
    {
        _pod1 = pod1;
        _pod2 = CreatePod(createAdditionalPod);
        _pod3 = CreatePod(createAdditionalPod);
    }

    public WorkflowServer Pod1
    {
        get
        {
            lock (_lifecycleLock)
            {
                ObjectDisposedException.ThrowIf(_disposed != 0, this);
                return _pod1;
            }
        }
    }

    public Task<WorkflowServer> GetPod2Async()
    {
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            return _pod2.Value;
        }
    }

    public Task<WorkflowServer> GetPod3Async()
    {
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            return _pod3.Value;
        }
    }

    public async Task<WorkflowServer[]> GetAllPodsAsync()
    {
        Task<WorkflowServer> pod2;
        Task<WorkflowServer> pod3;
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            pod2 = _pod2.Value;
            pod3 = _pod3.Value;
        }

        return [_pod1, await pod2, await pod3];
    }

    public async ValueTask DisposeAsync()
    {
        Task<WorkflowServer>? pod3;
        Task<WorkflowServer>? pod2;
        lock (_lifecycleLock)
        {
            if (_disposed != 0)
                return;

            _disposed = 1;
            pod3 = _pod3.IsValueCreated ? _pod3.Value : null;
            pod2 = _pod2.IsValueCreated ? _pod2.Value : null;
        }

        List<Exception>? failures = null;

        foreach (var pod in new[] { pod3, pod2 })
        {
            if (pod is null)
                continue;

            try
            {
                await (await pod).DisposeAsync();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        try
        {
            await _pod1.DisposeAsync();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        if (failures is { Count: > 0 })
            throw new AggregateException("Failed to dispose one or more component-test pods.", failures);
    }

    private static Lazy<Task<WorkflowServer>> CreatePod(Func<Task<WorkflowServer>> factory) =>
        new(factory, LazyThreadSafetyMode.ExecutionAndPublication);
}
