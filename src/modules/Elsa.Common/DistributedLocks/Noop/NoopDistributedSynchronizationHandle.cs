using Medallion.Threading;

namespace Elsa.Common.DistributedLocks.Noop;

/// <summary>
/// Represents a Noop distributed synchronization handle. This handle is used in the NoopDistributedLock implementation
/// to provide a no-operation implementation of the IDistributedSynchronizationHandle interface.
/// </summary>
public class NoopDistributedSynchronizationHandle : IDistributedSynchronizationHandle
{
    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return new ValueTask();
    }

    /// <inheritdoc />
    public CancellationToken HandleLostToken { get; }
}