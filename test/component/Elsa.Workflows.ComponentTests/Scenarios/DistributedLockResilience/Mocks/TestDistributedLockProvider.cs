using System.Collections.Concurrent;
using Medallion.Threading;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;

/// <summary>
/// Test implementation of IDistributedLockProvider that allows simulating transient failures.
/// </summary>
public class TestDistributedLockProvider(IDistributedLockProvider innerProvider) : IDistributedLockProvider
{
    private int _acquisitionFailuresRemaining;
    private int _releaseFailuresRemaining;
    private int _acquisitionAttemptCount;
    private int _releaseAttemptCount;
    private readonly ConcurrentDictionary<string, int> _lockSpecificFailures = new();
    private string? _targetLockPrefix;

    public int AcquisitionAttemptCount => _acquisitionAttemptCount;
    public int ReleaseAttemptCount => _releaseAttemptCount;

    public void FailAcquisitionOnce() => Interlocked.Exchange(ref _acquisitionFailuresRemaining, 1);
    public void FailAcquisitionTimes(int count) => Interlocked.Exchange(ref _acquisitionFailuresRemaining, count);
    public void FailReleaseOnce() => Interlocked.Exchange(ref _releaseFailuresRemaining, 1);

    /// <summary>
    /// Configure failures for a specific lock name prefix. Only locks matching this prefix will fail.
    /// </summary>
    public void FailAcquisitionTimesForLock(string lockNamePrefix, int count)
    {
        _targetLockPrefix = lockNamePrefix;
        Interlocked.Exchange(ref _acquisitionFailuresRemaining, count);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _acquisitionFailuresRemaining, 0);
        Interlocked.Exchange(ref _releaseFailuresRemaining, 0);
        Interlocked.Exchange(ref _acquisitionAttemptCount, 0);
        Interlocked.Exchange(ref _releaseAttemptCount, 0);
        _lockSpecificFailures.Clear();
        _targetLockPrefix = null;
    }

    public IDistributedLock CreateLock(string name)
    {
        return new TestDistributedLock(innerProvider.CreateLock(name), this, name);
    }

    internal bool ShouldFailAcquisition(string lockName)
    {
        Interlocked.Increment(ref _acquisitionAttemptCount);

        // If a target lock prefix is configured, only fail locks matching that prefix
        if (_targetLockPrefix != null && !lockName.StartsWith(_targetLockPrefix))
            return false;

        // Atomically decrement if > 0 using compare-exchange loop
        int currentValue, newValue;
        do
        {
            currentValue = Volatile.Read(ref _acquisitionFailuresRemaining);
            if (currentValue <= 0)
                return false;

            newValue = currentValue - 1;
        } while (Interlocked.CompareExchange(ref _acquisitionFailuresRemaining, newValue, currentValue) != currentValue);

        return true;
    }

    internal bool ShouldFailRelease()
    {
        Interlocked.Increment(ref _releaseAttemptCount);

        // Atomically decrement if > 0 using compare-exchange loop
        int currentValue, newValue;
        do
        {
            currentValue = Volatile.Read(ref _releaseFailuresRemaining);
            if (currentValue <= 0)
                return false;

            newValue = currentValue - 1;
        } while (Interlocked.CompareExchange(ref _releaseFailuresRemaining, newValue, currentValue) != currentValue);

        return true;
    }
}
