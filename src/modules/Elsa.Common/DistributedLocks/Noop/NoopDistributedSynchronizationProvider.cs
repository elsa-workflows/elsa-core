using Medallion.Threading;

namespace Elsa.Common.DistributedLocks.Noop;

/// <summary>
/// Represents a no-op implementation of the IDistributedLockProvider interface.
/// </summary>
public class NoopDistributedSynchronizationProvider : IDistributedLockProvider
{
    /// <inheritdoc />
    public IDistributedLock CreateLock(string name)
    {
        return new NoopDistributedLock();
    }
}