namespace Elsa.Workflows.Runtime;

/// <summary>
/// The result of an activation-gate evaluation. Dispose after the new instance is persisted
/// so the uniqueness check becomes visible to waiters.
/// </summary>
public sealed class WorkflowActivationLease : IAsyncDisposable
{
    private IAsyncDisposable? _lockHandle;
    private CancellationTokenSource? _linkedCancellationTokenSource;
    private readonly CancellationToken _effectiveCancellationToken;

    /// <summary>
    /// A lease that denies activation. No lock is held.
    /// </summary>
    public static WorkflowActivationLease Denied { get; } = new(false, null);

    public WorkflowActivationLease(bool canStart, IAsyncDisposable? lockHandle)
        : this(canStart, lockHandle, CancellationToken.None, null)
    {
    }

    internal WorkflowActivationLease(bool canStart, IAsyncDisposable? lockHandle, CancellationToken effectiveCancellationToken)
        : this(canStart, lockHandle, effectiveCancellationToken, null)
    {
    }

    internal WorkflowActivationLease(bool canStart, IAsyncDisposable? lockHandle, CancellationToken effectiveCancellationToken, CancellationTokenSource? linkedCancellationTokenSource)
    {
        CanStart = canStart;
        _lockHandle = lockHandle;
        _effectiveCancellationToken = effectiveCancellationToken;
        _linkedCancellationTokenSource = linkedCancellationTokenSource;
    }

    /// <summary>
    /// Whether a new workflow instance may be created.
    /// </summary>
    public bool CanStart { get; }

    internal CancellationToken GetEffectiveCancellationToken(CancellationToken fallbackToken) =>
        _effectiveCancellationToken.CanBeCanceled ? _effectiveCancellationToken : fallbackToken;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var lockHandle = Interlocked.Exchange(ref _lockHandle, null);
        var linkedCancellationTokenSource = Interlocked.Exchange(ref _linkedCancellationTokenSource, null);

        try
        {
            if (lockHandle != null)
                await lockHandle.DisposeAsync();
        }
        finally
        {
            linkedCancellationTokenSource?.Dispose();
        }
    }
}
