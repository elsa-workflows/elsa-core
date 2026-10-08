using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Stores.InMemory;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class InMemoryExternalAuthenticationSessionStoreTests
{
    private readonly DateTimeOffset _now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RotateRefreshTokenAtomicallyUpdatesGenerationAndReturnsACopy()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        await store.SaveAsync(ExternalAuthenticationTestData.CreateSession(_now));

        var result = await store.TryRotateRefreshTokenAsync("session-a", "refresh-1", 0, "refresh-2", _now.AddMinutes(1));
        var rotated = Assert.IsType<ExternalAuthenticationSessionRotationResult.Rotated>(result).Session;
        rotated.UserId = "modified";
        var reloaded = await store.FindByIdAsync("session-a");

        Assert.Equal(1, rotated.RefreshGeneration);
        Assert.Equal("refresh-2", rotated.CurrentRefreshTokenHash);
        Assert.NotNull(reloaded);
        Assert.Equal("user-a", reloaded.UserId);
        Assert.Equal("refresh-2", reloaded.CurrentRefreshTokenHash);
        Assert.Equal(1, reloaded.RefreshGeneration);
    }

    [Fact]
    public async Task SessionWithoutAnIssuedRefreshTokenCannotBeFoundByARefreshTokenHash()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        var session = ExternalAuthenticationTestData.CreateSession(_now);
        session.CurrentRefreshTokenHash = null!;
        await store.SaveAsync(session);

        var persisted = await store.FindByIdAsync(session.Id);
        var matched = await store.FindByRefreshTokenHashAsync(null!);

        Assert.Null(persisted!.CurrentRefreshTokenHash);
        Assert.Null(matched);
    }

    [Fact]
    public async Task SavingASessionWithAnExistingRefreshTokenHashIsRejectedAcrossTenants()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        var first = ExternalAuthenticationTestData.CreateSession(_now);
        var second = ExternalAuthenticationTestData.CreateSession(_now);
        second.Id = "session-b";
        second.TenantId = "tenant-b";
        await store.SaveAsync(first);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(second).AsTask());

        Assert.Null(await store.FindByIdAsync(second.Id));
        Assert.Equal(first.Id, (await store.FindByRefreshTokenHashAsync(first.CurrentRefreshTokenHash!))!.Id);
    }

    [Fact]
    public async Task SavingASessionWithAnExistingRefreshTokenHashFailsWithoutMutatingEitherSession()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        var first = ExternalAuthenticationTestData.CreateSession(_now);
        var second = ExternalAuthenticationTestData.CreateSession(_now);
        second.Id = "session-b";
        second.CurrentRefreshTokenHash = null;
        await store.SaveAsync(first);
        await store.SaveAsync(second);

        second.CurrentRefreshTokenHash = first.CurrentRefreshTokenHash;
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(second).AsTask());

        var persistedFirst = await store.FindByIdAsync(first.Id);
        var persistedSecond = await store.FindByIdAsync(second.Id);
        Assert.NotNull(persistedFirst);
        Assert.NotNull(persistedSecond);
        Assert.Equal(first.CurrentRefreshTokenHash, persistedFirst.CurrentRefreshTokenHash);
        Assert.Null(persistedSecond.CurrentRefreshTokenHash);
        Assert.Equal(first.Id, (await store.FindByRefreshTokenHashAsync(first.CurrentRefreshTokenHash!))!.Id);
    }

    [Fact]
    public async Task RotatingToAnExistingRefreshTokenHashFailsWithoutMutatingEitherSession()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        var first = ExternalAuthenticationTestData.CreateSession(_now);
        var second = ExternalAuthenticationTestData.CreateSession(_now);
        second.Id = "session-b";
        second.CurrentRefreshTokenHash = "refresh-2";
        await store.SaveAsync(first);
        await store.SaveAsync(second);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.TryRotateRefreshTokenAsync(first.Id, "refresh-1", 0, "refresh-2", _now.AddMinutes(1)).AsTask());

        var persistedFirst = await store.FindByIdAsync(first.Id);
        var persistedSecond = await store.FindByIdAsync(second.Id);
        Assert.NotNull(persistedFirst);
        Assert.NotNull(persistedSecond);
        Assert.Equal("refresh-1", persistedFirst.CurrentRefreshTokenHash);
        Assert.Equal(0, persistedFirst.RefreshGeneration);
        Assert.Equal(_now, persistedFirst.LastRefreshedAt);
        Assert.Equal("refresh-2", persistedSecond.CurrentRefreshTokenHash);
        Assert.Equal(second.Id, (await store.FindByRefreshTokenHashAsync("refresh-2"))!.Id);
    }

    [Fact]
    public async Task ReusingASupersededRefreshTokenRevokesTheSession()
    {
        var clock = new TestSystemClock(_now);
        var store = new InMemoryExternalAuthenticationSessionStore(clock);
        await store.SaveAsync(ExternalAuthenticationTestData.CreateSession(_now));
        await store.TryRotateRefreshTokenAsync("session-a", "refresh-1", 0, "refresh-2", _now.AddMinutes(1));

        var replay = await store.TryRotateRefreshTokenAsync("session-a", "refresh-1", 0, "refresh-3", _now.AddMinutes(2));
        var currentTokenAttempt = await store.TryRotateRefreshTokenAsync("session-a", "refresh-2", 1, "refresh-3", _now.AddMinutes(2));
        var session = await store.FindByIdAsync("session-a");

        Assert.IsType<ExternalAuthenticationSessionRotationResult.Reused>(replay);
        Assert.IsType<ExternalAuthenticationSessionRotationResult.Revoked>(currentTokenAttempt);
        Assert.NotNull(session);
        Assert.Equal(clock.UtcNow, session.RevokedAt);
        Assert.Equal("refresh_token_reuse", session.RevocationReason);
    }

    [Fact]
    public async Task ExpiredSessionCannotBeRotated()
    {
        var clock = new TestSystemClock(_now);
        var session = ExternalAuthenticationTestData.CreateSession(_now);
        session.RefreshExpiresAt = _now.AddSeconds(1);
        var store = new InMemoryExternalAuthenticationSessionStore(clock);
        await store.SaveAsync(session);
        clock.UtcNow = _now.AddSeconds(1);

        var result = await store.TryRotateRefreshTokenAsync(session.Id, "refresh-1", 0, "refresh-2", clock.UtcNow);

        Assert.IsType<ExternalAuthenticationSessionRotationResult.Expired>(result);
    }

    [Fact]
    public async Task ExplicitRevocationIsACompareAndSetOperation()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        await store.SaveAsync(ExternalAuthenticationTestData.CreateSession(_now));

        var first = await store.RevokeAsync("session-a", "administrator", _now.AddMinutes(1));
        var second = await store.RevokeAsync("session-a", "administrator", _now.AddMinutes(1));

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task ConcurrentRefreshRotationsPermitOnlyOneWinner()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        await store.SaveAsync(ExternalAuthenticationTestData.CreateSession(_now));

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index =>
            store.TryRotateRefreshTokenAsync("session-a", "refresh-1", 0, $"refresh-{index + 2}", _now.AddMinutes(1)).AsTask()));

        Assert.Single(results.OfType<ExternalAuthenticationSessionRotationResult.Rotated>());
        Assert.Contains(results, result => result is ExternalAuthenticationSessionRotationResult.Reused);
        var session = await store.FindByIdAsync("session-a");
        Assert.NotNull(session);
        Assert.Equal("refresh_token_reuse", session.RevocationReason);
    }
}
