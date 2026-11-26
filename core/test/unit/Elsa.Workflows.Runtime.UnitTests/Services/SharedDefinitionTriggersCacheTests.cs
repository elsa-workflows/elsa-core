using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

/// <summary>
/// Tenant activations share the work of <see cref="SharedDefinitionTriggersCache"/>; one activation that is cancelled or goes away must
/// not fail the others.
/// </summary>
public class SharedDefinitionTriggersCacheTests
{
    private static readonly SharedDefinitionTriggersCache.DerivedTriggers Derived = new(new Workflow(), [], []);
    private readonly SharedDefinitionTriggersCache _cache = new();

    [Fact(DisplayName = "A caller that is cancelled stops waiting, and another caller still gets the shared result")]
    public async Task CancelledCaller_DoesNotFailOtherCallers()
    {
        var release = new TaskCompletionSource();
        var attempts = 0;
        using var cancelledCaller = new CancellationTokenSource();

        async Task<SharedDefinitionTriggersCache.DerivedTriggers> DeriveAsync(CancellationToken sharedToken)
        {
            Interlocked.Increment(ref attempts);
            await release.Task.WaitAsync(sharedToken);
            return Derived;
        }

        var first = _cache.GetOrDeriveAsync("version", DeriveAsync, null, cancelledCaller.Token);
        var second = _cache.GetOrDeriveAsync("version", DeriveAsync, null, CancellationToken.None);
        await cancelledCaller.CancelAsync();
        release.SetResult();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.Same(Derived, await second);
        Assert.Equal(1, attempts);
    }

    [Fact(DisplayName = "When the attempt that another caller started fails, a waiting caller tries again on its own")]
    public async Task FailedForeignAttempt_IsRetriedByWaitingCaller()
    {
        var failFirstAttempt = new TaskCompletionSource();
        var first = _cache.EnsureSharedRowsReconciledAsync("version", async _ =>
        {
            await failFirstAttempt.Task;
            throw new ObjectDisposedException("scope");
        }, null, CancellationToken.None);
        var second = _cache.EnsureSharedRowsReconciledAsync("version", _ => Task.FromResult(true), null, CancellationToken.None);

        failFirstAttempt.SetResult();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => first);
        Assert.True(await second);
    }
}
