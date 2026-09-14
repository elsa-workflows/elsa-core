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
        var simulateFailure = provider.ShouldFailRelease();
        try
        {
            innerHandle?.Dispose();
        }
        catch (Exception disposalFailure) when (simulateFailure)
        {
            throw new AggregateException(
                new TimeoutException("Simulated transient timeout during lock release"),
                disposalFailure);
        }

        if (simulateFailure)
            throw new TimeoutException("Simulated transient timeout during lock release");
    }

    public async ValueTask DisposeAsync()
    {
        var simulateFailure = provider.ShouldFailRelease();
        try
        {
            if (innerHandle is not null)
                await innerHandle.DisposeAsync();
        }
        catch (Exception disposalFailure) when (simulateFailure)
        {
            throw new AggregateException(
                new TimeoutException("Simulated transient timeout during lock release"),
                disposalFailure);
        }

        if (simulateFailure)
            throw new TimeoutException("Simulated transient timeout during lock release");
    }
}
