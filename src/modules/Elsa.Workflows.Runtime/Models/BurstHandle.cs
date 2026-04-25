namespace Elsa.Workflows.Runtime;

/// <summary>
/// Tracks a single in-flight burst of workflow execution. Created when a burst starts, disposed when it completes
/// or is force-cancelled during drain. Active-burst accounting is done through <c>IBurstRegistry</c>.
/// </summary>
public sealed class BurstHandle : IDisposable
{
    private readonly CancellationTokenSource _burstCts;
    private readonly Action<BurstHandle>? _onDisposed;
    private int _disposed;

    /// <summary>
    /// Creates a new handle. The owning <c>IBurstRegistry</c> supplies the disposal callback so it can decrement its active count.
    /// </summary>
    public BurstHandle(Guid id, string workflowInstanceId, string? ingressSourceName, DateTimeOffset startedAt, CancellationToken linkedToken, Action<BurstHandle>? onDisposed = null)
    {
        Id = id;
        WorkflowInstanceId = workflowInstanceId;
        IngressSourceName = ingressSourceName;
        StartedAt = startedAt;
        _burstCts = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
        _onDisposed = onDisposed;
    }

    /// <summary>Unique identifier for this burst within the current runtime generation.</summary>
    public Guid Id { get; }

    /// <summary>Workflow instance whose execution this burst is driving.</summary>
    public string WorkflowInstanceId { get; }

    /// <summary>
    /// Name of the ingress source that initiated this burst, when attribution is available (null for direct API invocations).
    /// </summary>
    public string? IngressSourceName { get; }

    /// <summary>When the burst started.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Cancellation token passed into the workflow execution pipeline for this burst.</summary>
    public CancellationToken CancellationToken => _burstCts.Token;

    /// <summary>
    /// Cancels the burst — used by the drain orchestrator on deadline breach or operator force.
    /// Safe to call multiple times; idempotent.
    /// </summary>
    public void Cancel()
    {
        if (_disposed != 0) return;
        try { _burstCts.Cancel(); }
        catch (ObjectDisposedException) { /* race with Dispose — acceptable */ }
    }

    /// <summary>Releases the linked CTS and notifies the registry.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _onDisposed?.Invoke(this);
        _burstCts.Dispose();
    }
}
