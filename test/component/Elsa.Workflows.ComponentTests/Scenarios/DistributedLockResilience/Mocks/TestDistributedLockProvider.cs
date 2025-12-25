using JetBrains.Annotations;
using Medallion.Threading;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;

/// <summary>
/// Test implementation of IDistributedLockProvider that allows simulating transient failures.
/// </summary>
[UsedImplicitly]
public class TestDistributedLockProvider(IDistributedLockProvider innerProvider) : IDistributedLockProvider
{
    private int _acquisitionFailuresRemaining;
    private int _releaseFailuresRemaining;
    private int _acquisitionAttemptCount;
    private int _releaseAttemptCount;
    private string? _targetLockPrefix;

    public int AcquisitionAttemptCount => _acquisitionAttemptCount;
    public int ReleaseAttemptCount => _releaseAttemptCount;

    public void FailAcquisitionOnce() => ConfigureAcquisitionFailures(1);
    public void FailAcquisitionTimes(int count) => ConfigureAcquisitionFailures(count);
    public void FailReleaseOnce() => Interlocked.Exchange(ref _releaseFailuresRemaining, 1);

    /// <summary>
    /// Configure failures for a specific lock name prefix. Only locks matching this prefix will fail.
    /// </summary>
    public void FailAcquisitionTimesForLock(string lockNamePrefix, int count)
    {
        _targetLockPrefix = lockNamePrefix;
        ConfigureAcquisitionFailures(count);
    }

    public void Reset()
    {
        ConfigureAcquisitionFailures(0);
        Interlocked.Exchange(ref _releaseFailuresRemaining, 0);
        Interlocked.Exchange(ref _acquisitionAttemptCount, 0);
        Interlocked.Exchange(ref _releaseAttemptCount, 0);
        _targetLockPrefix = null;
    }

    public IDistributedLock CreateLock(string name) =>
        new TestDistributedLock(innerProvider.CreateLock(name), this, name);

    internal bool ShouldFailAcquisition(string lockName)
    {
        Interlocked.Increment(ref _acquisitionAttemptCount);

        // If a target lock prefix is configured, only fail locks matching that prefix
        if (_targetLockPrefix != null && !lockName.StartsWith(_targetLockPrefix))
            return false;

        return TryConsumeFailure(ref _acquisitionFailuresRemaining);
    }

    internal bool ShouldFailRelease()
    {
        Interlocked.Increment(ref _releaseAttemptCount);
        return TryConsumeFailure(ref _releaseFailuresRemaining);
    }

    private void ConfigureAcquisitionFailures(int count) =>
        Interlocked.Exchange(ref _acquisitionFailuresRemaining, count);

    /// <summary>
    /// Atomically decrements the failure counter if it's greater than 0.
    /// Returns true if a failure was consumed, false otherwise.
    /// </summary>
    private static bool TryConsumeFailure(ref int failureCounter)
    {
        int currentValue, newValue;
        do
        {
            currentValue = Volatile.Read(ref failureCounter);
            if (currentValue <= 0)
                return false;

            newValue = currentValue - 1;
        } while (Interlocked.CompareExchange(ref failureCounter, newValue, currentValue) != currentValue);

        return true;
    }
}
