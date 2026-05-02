using System.Collections.Concurrent;
using Elsa.Common;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default implementation of <see cref="IExecutionCycleRegistry"/>. Tracks active execution cycles with a
/// thread-safe dictionary and detects the FR-018 invariant violation (a source that reports
/// <see cref="IngressSourceState.Paused"/> but initiates an execution cycle).
/// </summary>
public sealed class ExecutionCycleRegistry : IExecutionCycleRegistry
{
    private readonly ConcurrentDictionary<Guid, ExecutionCycleHandle> _active = new();
    private readonly IIngressSourceRegistry _sources;
    private readonly ISystemClock _clock;

    public ExecutionCycleRegistry(IIngressSourceRegistry sources, ISystemClock clock)
    {
        _sources = sources;
        _clock = clock;
    }

    /// <inheritdoc />
    public int ActiveCount => _active.Count;

    /// <inheritdoc />
    public ExecutionCycleHandle BeginCycle(string workflowInstanceId, string? ingressSourceName, CancellationToken linkedToken, Action? cancelCallback = null)
    {
        // FR-018: a source that reports Paused but starts an execution cycle is inconsistent — flip it to PauseFailed.
        if (ingressSourceName is not null)
        {
            var snapshot = _sources.Snapshot().FirstOrDefault(s => s.Name == ingressSourceName);
            if (snapshot is not null && snapshot.State == IngressSourceState.Paused)
            {
                // Fire-and-forget: flipping the recorded state is a local operation that does not await I/O.
                _ = _sources.MarkPauseFailedAsync(ingressSourceName, reason: "delivered-while-paused");
            }
        }

        var handle = new ExecutionCycleHandle(
            id: Guid.NewGuid(),
            workflowInstanceId: workflowInstanceId,
            ingressSourceName: ingressSourceName,
            startedAt: _clock.UtcNow,
            linkedToken: linkedToken,
            onDisposed: h => _active.TryRemove(h.Id, out _),
            cancelCallback: cancelCallback);

        _active[handle.Id] = handle;
        return handle;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ExecutionCycleHandle> ListActiveCycles() => _active.Values.ToArray();
}
