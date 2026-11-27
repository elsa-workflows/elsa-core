using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

/// <summary>
/// Tenant activations share the work of <see cref="SharedDefinitionTriggersCache"/>; one activation that is cancelled or goes away must
/// not fail the others, and nothing derived from a definition read before the version was forgotten is shared.
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

        var first = _cache.GetOrDeriveAsync("version", _cache.GetGeneration("version"), DeriveAsync, null, cancelledCaller.Token);
        var second = _cache.GetOrDeriveAsync("version", _cache.GetGeneration("version"), DeriveAsync, null, CancellationToken.None);
        await cancelledCaller.CancelAsync();
        release.SetResult();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.Same(Derived, await second);
        Assert.Equal(1, attempts);
    }

    [Fact(DisplayName = "When the attempt that another caller started fails, a waiting caller tries again on its own, and only then")]
    public async Task FailedForeignAttempt_IsRetriedByWaitingCaller()
    {
        var failFirstAttempt = new TaskCompletionSource();
        var secondAttempts = 0;
        var first = _cache.EnsureSharedRowsReconciledAsync("version", async _ =>
        {
            await failFirstAttempt.Task;
            throw new ObjectDisposedException("scope");
        }, null, CancellationToken.None);
        var second = _cache.EnsureSharedRowsReconciledAsync("version", _ =>
        {
            Interlocked.Increment(ref secondAttempts);
            return Task.FromResult(true);
        }, null, CancellationToken.None);

        Assert.Equal(0, secondAttempts);
        failFirstAttempt.SetResult();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => first);
        Assert.True(await second);
        Assert.Equal(1, secondAttempts);
    }

    [Fact(DisplayName = "Forgetting a version drops both its derived triggers and its reconciled shared rows")]
    public async Task Forget_ClearsDerivedTriggersAndReconciledVersion()
    {
        var reconciliations = 0;
        await _cache.GetOrDeriveAsync("version", _cache.GetGeneration("version"), _ => Task.FromResult(Derived), null, CancellationToken.None);
        await _cache.EnsureSharedRowsReconciledAsync("version", _ => Task.FromResult(++reconciliations > 0), null, CancellationToken.None);

        _cache.Forget("version");

        Assert.False(_cache.ContainsDerived("version", null));
        await _cache.EnsureSharedRowsReconciledAsync("version", _ => Task.FromResult(++reconciliations > 0), null, CancellationToken.None);
        Assert.Equal(2, reconciliations);
    }

    [Fact(DisplayName = "Triggers derived from a definition read before the version was forgotten are not shared afterwards")]
    public async Task DerivationFromReadBeforeForget_IsNotShared()
    {
        var stale = new SharedDefinitionTriggersCache.DerivedTriggers(new Workflow(), [], []);
        var current = new SharedDefinitionTriggersCache.DerivedTriggers(new Workflow(), [], []);
        var generationBeforeRead = _cache.GetGeneration("version");

        _cache.Forget("version");
        var returnedToStaleReader = await _cache.GetOrDeriveAsync("version", generationBeforeRead, _ => Task.FromResult(stale), null, CancellationToken.None);
        var returnedToNextReader = await _cache.GetOrDeriveAsync("version", _cache.GetGeneration("version"), _ => Task.FromResult(current), null, CancellationToken.None);

        Assert.Same(stale, returnedToStaleReader);
        Assert.Same(current, returnedToNextReader);
    }
}
