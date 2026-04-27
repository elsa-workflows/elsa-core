namespace Elsa.Workflows.Runtime;

/// <summary>
/// Tracks a single in-flight burst of workflow execution. Created when a burst starts, disposed when it completes
/// or is force-cancelled during drain. Active-burst accounting is done through <c>IBurstRegistry</c>.
/// </summary>
public sealed class BurstHandle : IDisposable
{
    private readonly CancellationTokenSource _burstCts;
    private readonly Action<BurstHandle>? _onDisposed;
    private readonly Action? _cancelCallback;
    private readonly TaskCompletionSource _disposedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _disposed;

    /// <summary>
    /// Creates a new handle. The owning <c>IBurstRegistry</c> supplies <paramref name="onDisposed"/> so it can decrement its active count.
    /// The optional <paramref name="cancelCallback"/> is invoked by <see cref="Cancel"/> to propagate cancellation
    /// into the workflow execution itself (e.g., <c>WorkflowExecutionContext.Cancel()</c>) so that the running burst
    /// stops scheduling new activities. Without it, <see cref="Cancel"/> only signals the burst's own
    /// <see cref="CancellationToken"/>, which is consumed by the burst registry but does NOT propagate to the
    /// workflow runner's pipeline (the runner reads from <c>WorkflowExecutionContext.CancellationToken</c>, which
    /// is captured at context construction and is not part of this linked chain).
    /// </summary>
    public BurstHandle(
        Guid id,
        string workflowInstanceId,
        string? ingressSourceName,
        DateTimeOffset startedAt,
        CancellationToken linkedToken,
        Action<BurstHandle>? onDisposed = null,
        Action? cancelCallback = null)
    {
        Id = id;
        WorkflowInstanceId = workflowInstanceId;
        IngressSourceName = ingressSourceName;
        StartedAt = startedAt;
        _burstCts = CancellationTokenSource.CreateLinkedTokenSource(linkedToken);
        _onDisposed = onDisposed;
        _cancelCallback = cancelCallback;
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
    /// Completes when <see cref="Dispose"/> runs — i.e., when the workflow runner finishes the burst (cleanly or
    /// via cancellation) and the middleware exits its <c>using</c> block. The drain orchestrator awaits this with
    /// a timeout before persisting <see cref="WorkflowSubStatus.Interrupted"/>, ensuring its write happens AFTER
    /// any commit the runner emits in response to <see cref="Cancel"/>.
    /// </summary>
    public Task Disposed => _disposedTcs.Task;

    /// <summary>
    /// Cancels the burst — used by the drain orchestrator on deadline breach or operator force.
    /// Invokes the cancel callback (when supplied at construction) to propagate cancellation into the workflow
    /// execution, then cancels the burst's own linked CTS. Safe to call multiple times; idempotent.
    /// </summary>
    public void Cancel()
    {
        if (_disposed != 0) return;

        // Propagate to the workflow execution first (this typically marks the workflow as Cancelled and clears its
        // schedule, so the runner stops scheduling new activities). The orchestrator's subsequent Interrupted
        // persistence then overrides the Cancelled sub-status — see Disposed-await sequencing in DrainOrchestrator.
        try { _cancelCallback?.Invoke(); }
        catch { /* Cancellation is best-effort; failures here must not break the drain. */ }

        try { _burstCts.Cancel(); }
        catch (ObjectDisposedException) { /* Race with Dispose — acceptable. */ }
    }

    /// <summary>Releases the linked CTS, notifies the registry, and signals <see cref="Disposed"/>.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _onDisposed?.Invoke(this);
        _burstCts.Dispose();
        _disposedTcs.TrySetResult();
    }
}
