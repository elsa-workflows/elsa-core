using Medallion.Threading;

namespace Elsa.Common.DistributedHosting.DistributedLocks;

/// <summary>
/// Represents a Noop (No-Operation) implementation of the <see cref="IDistributedLock"/> interface.
/// </summary>
public class NoopDistributedLock : IDistributedLock
{
    /// <inheritdoc />
    public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = new TimeSpan(), CancellationToken cancellationToken = new CancellationToken())
    {
        return new NoopDistributedSynchronizationHandle();
    }

    /// <inheritdoc />
    public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = new CancellationToken())
    {
        return new NoopDistributedSynchronizationHandle();
    }

    /// <inheritdoc />
    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = new TimeSpan(), CancellationToken cancellationToken = new CancellationToken())
    {
        return new ValueTask<IDistributedSynchronizationHandle?>(new NoopDistributedSynchronizationHandle());
    }

    /// <inheritdoc />
    public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = new CancellationToken())
    {
        return new ValueTask<IDistributedSynchronizationHandle>(new NoopDistributedSynchronizationHandle());
    }

    /// <inheritdoc />
    public string Name => "Noop";
}