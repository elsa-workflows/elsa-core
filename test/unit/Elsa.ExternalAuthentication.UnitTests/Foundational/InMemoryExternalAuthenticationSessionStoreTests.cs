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
