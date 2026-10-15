using Medallion.Threading;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;

/// <summary>
/// Test implementation of IDistributedSynchronizationHandle that can simulate failures on disposal.
/// </summary>
public class TestDistributedSynchronizationHandle(
    IDistributedSynchronizationHandle? innerHandle,
    TestDistributedLockProvider provider) : IDistributedSynchronizationHandle
{
    public CancellationToken HandleLostToken => innerHandle?.HandleLostToken ?? CancellationToken.None;

    public void Dispose()
    {
        if (provider.ShouldFailRelease())
        {
            throw new TimeoutException("Simulated transient timeout during lock release");
        }
        innerHandle?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        if (provider.ShouldFailRelease())
        {
            throw new TimeoutException("Simulated transient timeout during lock release");
        }
        return innerHandle?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
