namespace Elsa.Workflows.Runtime;

/// <summary>
/// The result of an activation-gate evaluation. Dispose after the new instance is persisted
/// so the uniqueness check becomes visible to waiters.
/// </summary>
public sealed class WorkflowActivationLease : IAsyncDisposable
{
    private readonly IAsyncDisposable? _lockHandle;

    /// <summary>
    /// A lease that denies activation. No lock is held.
    /// </summary>
    public static WorkflowActivationLease Denied { get; } = new(false, null);

    public WorkflowActivationLease(bool canStart, IAsyncDisposable? lockHandle)
    {
        CanStart = canStart;
        _lockHandle = lockHandle;
    }

    /// <summary>
    /// Whether a new workflow instance may be created.
    /// </summary>
    public bool CanStart { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_lockHandle != null)
            await _lockHandle.DisposeAsync();
    }
}
