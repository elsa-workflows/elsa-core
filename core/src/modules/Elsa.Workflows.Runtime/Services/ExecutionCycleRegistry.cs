using System.Collections.Concurrent;
using Elsa.Common;
using Elsa.Common.Multitenancy;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Default implementation of <see cref="IExecutionCycleRegistry"/>. Tracks active execution cycles with a
/// thread-safe dictionary and detects the FR-018 invariant violation (a source that reports
/// <see cref="IngressSourceState.Paused"/> but initiates an execution cycle). Each handle records the tenant that is
/// current when its cycle begins, so the drain can persist the instance under that tenant.
/// </summary>
public sealed class ExecutionCycleRegistry : IExecutionCycleRegistry
{
    private readonly ConcurrentDictionary<Guid, ExecutionCycleHandle> _active = new();
    private readonly IIngressSourceRegistry _sources;
    private readonly ISystemClock _clock;
    private readonly ITenantAccessor? _tenantAccessor;

    [Obsolete("Use the constructor that accepts an ITenantAccessor instead. Without it, a drain cannot persist the instances of named tenants. Removed in the next major version.")]
    public ExecutionCycleRegistry(IIngressSourceRegistry sources, ISystemClock clock) : this(sources, clock, null)
    {
    }

    public ExecutionCycleRegistry(IIngressSourceRegistry sources, ISystemClock clock, ITenantAccessor? tenantAccessor)
    {
        _sources = sources;
        _clock = clock;
        _tenantAccessor = tenantAccessor;
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
            cancelCallback: cancelCallback)
        {
            // Recorded before the handle is registered, so a drain never sees it without its tenant.
            Tenant = _tenantAccessor?.Tenant
        };

        _active[handle.Id] = handle;
        return handle;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<ExecutionCycleHandle> ListActiveCycles() => _active.Values.ToArray();
}
