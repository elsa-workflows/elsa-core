using Medallion.Threading;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;

/// <summary>
/// Test implementation of IDistributedLockProvider that allows simulating transient failures.
/// </summary>
public class TestDistributedLockProvider : IDistributedLockProvider
{
    private readonly IDistributedLockProvider _innerProvider;
    private int _acquisitionFailuresRemaining;
    private int _releaseFailuresRemaining;
    private int _acquisitionAttemptCount;
    private int _releaseAttemptCount;

    public TestDistributedLockProvider(IDistributedLockProvider innerProvider)
    {
        _innerProvider = innerProvider;
    }

    public int AcquisitionAttemptCount => _acquisitionAttemptCount;
    public int ReleaseAttemptCount => _releaseAttemptCount;

    public void FailAcquisitionOnce() => _acquisitionFailuresRemaining = 1;
    public void FailAcquisitionTimes(int count) => _acquisitionFailuresRemaining = count;
    public void FailReleaseOnce() => _releaseFailuresRemaining = 1;
    public void FailReleaseTimes(int count) => _releaseFailuresRemaining = count;

    public void Reset()
    {
        _acquisitionFailuresRemaining = 0;
        _releaseFailuresRemaining = 0;
        _acquisitionAttemptCount = 0;
        _releaseAttemptCount = 0;
    }

    public IDistributedLock CreateLock(string name)
    {
        return new TestDistributedLock(
            _innerProvider.CreateLock(name),
            this);
    }

    internal bool ShouldFailAcquisition()
    {
        _acquisitionAttemptCount++;
        if (_acquisitionFailuresRemaining > 0)
        {
            _acquisitionFailuresRemaining--;
            return true;
        }
        return false;
    }

    internal bool ShouldFailRelease()
    {
        _releaseAttemptCount++;
        if (_releaseFailuresRemaining > 0)
        {
            _releaseFailuresRemaining--;
            return true;
        }
        return false;
    }
}
