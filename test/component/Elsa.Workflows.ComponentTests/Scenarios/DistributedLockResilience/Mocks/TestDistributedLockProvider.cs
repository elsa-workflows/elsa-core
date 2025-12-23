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

    public int AcquisitionAttemptCount => _acquisitionAttemptCount;
    public int ReleaseAttemptCount => _releaseAttemptCount;

    public void FailAcquisitionOnce() => _acquisitionFailuresRemaining = 1;
    public void FailAcquisitionTimes(int count) => _acquisitionFailuresRemaining = count;
    public void FailReleaseOnce() => _releaseFailuresRemaining = 1;

    public void Reset()
    {
        _acquisitionFailuresRemaining = 0;
        _releaseFailuresRemaining = 0;
        _acquisitionAttemptCount = 0;
        _releaseAttemptCount = 0;
    }

    public IDistributedLock CreateLock(string name)
    {
        return new TestDistributedLock(innerProvider.CreateLock(name), this);
    }

    internal bool ShouldFailAcquisition()
    {
        _acquisitionAttemptCount++;
        if (_acquisitionFailuresRemaining <= 0)
            return false;

        _acquisitionFailuresRemaining--;
        return true;
    }

    internal bool ShouldFailRelease()
    {
        _releaseAttemptCount++;
        if (_releaseFailuresRemaining <= 0)
            return false;

        _releaseFailuresRemaining--;
        return true;
    }
}
