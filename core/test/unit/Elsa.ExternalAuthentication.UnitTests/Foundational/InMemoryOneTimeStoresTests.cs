using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Stores.InMemory;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class InMemoryOneTimeStoresTests
{
    private readonly DateTimeOffset _now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StateStoreConsumesExactlyOneConcurrentAttempt()
    {
        var store = new InMemoryExternalAuthenticationStateStore(new TestSystemClock(_now));
        await store.PutAsync("sign-in", "state-hash", "payload", _now.AddMinutes(1));

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => store.TryTakeAsync<string>("sign-in", "state-hash").AsTask()));

        Assert.Single(results.OfType<TakeResult<string>.Taken>());
        Assert.Equal(15, results.OfType<TakeResult<string>.AlreadyConsumed>().Count());
    }

    [Fact]
    public async Task StateStoreReturnsExpiredWithoutRearmingTheHandle()
    {
        var clock = new TestSystemClock(_now);
        var store = new InMemoryExternalAuthenticationStateStore(clock);
        await store.PutAsync("sign-in", "state-hash", "payload", _now.AddSeconds(1));
        clock.UtcNow = _now.AddSeconds(1);

        var result = await store.TryTakeAsync<string>("sign-in", "state-hash");

        Assert.IsType<TakeResult<string>.Expired>(result);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.PutAsync("sign-in", "state-hash", "replacement", _now.AddMinutes(1)).AsTask());
    }

    [Fact]
    public async Task AuthorizationGrantStoreConsumesAClonedGrantOnlyOnce()
    {
        var store = new InMemoryAuthorizationGrantStore(new TestSystemClock(_now));
        var grant = ExternalAuthenticationTestData.CreateGrant(_now.AddMinutes(1));
        await store.SaveAsync(grant);
        grant.UserId = "changed-after-save";

        var first = await store.TryTakeAsync(grant.CodeHash);
        var second = await store.TryTakeAsync(grant.CodeHash);

        var taken = Assert.IsType<TakeResult<AuthorizationGrant>.Taken>(first);
        Assert.Equal("user-a", taken.Value.UserId);
        Assert.IsType<TakeResult<AuthorizationGrant>.AlreadyConsumed>(second);
    }

    [Fact]
    public async Task AuthorizationGrantStoreConsumesExactlyOneConcurrentAttempt()
    {
        var store = new InMemoryAuthorizationGrantStore(new TestSystemClock(_now));
        var grant = ExternalAuthenticationTestData.CreateGrant(_now.AddMinutes(1));
        await store.SaveAsync(grant);

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => store.TryTakeAsync(grant.CodeHash).AsTask()));

        Assert.Single(results.OfType<TakeResult<AuthorizationGrant>.Taken>());
        Assert.Equal(15, results.OfType<TakeResult<AuthorizationGrant>.AlreadyConsumed>().Count());
    }

    [Fact]
    public async Task PreviewResultStoreRestrictsResultsToTheInitiatingAdministratorAndConsumesOnce()
    {
        var store = new InMemoryPreviewResultStore(new TestSystemClock(_now));
        var preview = ExternalAuthenticationTestData.CreatePreview(_now.AddMinutes(1));
        await store.SaveAsync(preview);

        var unauthorized = await store.TryTakeAsync(preview.HandleHash, "administrator-b");
        var first = await store.TryTakeAsync(preview.HandleHash, preview.AdministratorId);
        var second = await store.TryTakeAsync(preview.HandleHash, preview.AdministratorId);

        Assert.IsType<TakeResult<PreviewResult>.NotFound>(unauthorized);
        Assert.IsType<TakeResult<PreviewResult>.Taken>(first);
        Assert.IsType<TakeResult<PreviewResult>.AlreadyConsumed>(second);
    }

    [Fact]
    public async Task PreviewResultStoreConsumesExactlyOneConcurrentAuthorizedAttempt()
    {
        var store = new InMemoryPreviewResultStore(new TestSystemClock(_now));
        var preview = ExternalAuthenticationTestData.CreatePreview(_now.AddMinutes(1));
        await store.SaveAsync(preview);

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => store.TryTakeAsync(preview.HandleHash, preview.AdministratorId).AsTask()));

        Assert.Single(results.OfType<TakeResult<PreviewResult>.Taken>());
        Assert.Equal(15, results.OfType<TakeResult<PreviewResult>.AlreadyConsumed>().Count());
    }
}
