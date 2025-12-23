using Medallion.Threading;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;

/// <summary>
/// Test implementation of IDistributedLock that delegates to an inner lock
/// but can simulate transient failures.
/// </summary>
public class TestDistributedLock(IDistributedLock innerLock, TestDistributedLockProvider provider) : IDistributedLock
{
    public string Name => innerLock.Name;

    public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (provider.ShouldFailAcquisition())
        {
            throw new TimeoutException("Simulated transient timeout during lock acquisition");
        }

        var handle = innerLock.Acquire(timeout, cancellationToken);
        return new TestDistributedSynchronizationHandle(handle, provider);
    }

    public async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (provider.ShouldFailAcquisition())
        {
            throw new TimeoutException("Simulated transient timeout during lock acquisition");
        }

        var handle = await innerLock.AcquireAsync(timeout, cancellationToken);
        return new TestDistributedSynchronizationHandle(handle, provider);
    }

    public async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        if (provider.ShouldFailAcquisition())
        {
            throw new TimeoutException("Simulated transient timeout during lock acquisition");
        }

        var handle = await innerLock.TryAcquireAsync(timeout, cancellationToken);
        return handle == null ? null : new TestDistributedSynchronizationHandle(handle, provider);
    }

    public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        if (provider.ShouldFailAcquisition())
        {
            throw new TimeoutException("Simulated transient timeout during lock acquisition");
        }

        var handle = innerLock.TryAcquire(timeout, cancellationToken);
        return handle == null ? null : new TestDistributedSynchronizationHandle(handle, provider);
    }
}
