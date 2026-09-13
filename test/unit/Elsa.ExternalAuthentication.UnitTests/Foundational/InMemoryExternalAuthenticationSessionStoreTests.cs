using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Stores.InMemory;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class InMemoryExternalAuthenticationSessionStoreTests
{
    private readonly DateTimeOffset _now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task RotateRefreshTokenAtomicallyUpdatesGenerationAndReturnsACopy()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        await store.SaveAsync(ExternalAuthenticationTestData.CreateSession(_now));

        var result = await store.TryRotateRefreshTokenAsync("session-a", "refresh-1", 0, "refresh-2", _now.AddMinutes(1));
        await Assert.That(result).IsOfType(typeof(ExternalAuthenticationSessionRotationResult.Rotated));
        var rotated = ((ExternalAuthenticationSessionRotationResult.Rotated)result).Session;
        rotated.UserId = "modified";
        var reloaded = await store.FindByIdAsync("session-a");

        await Assert.That(rotated.RefreshGeneration).IsEqualTo(1);
        await Assert.That(rotated.CurrentRefreshTokenHash).IsEqualTo("refresh-2");
        await Assert.That(reloaded).IsNotNull();
        var reloadedSession = reloaded!;
        await Assert.That(reloadedSession.UserId).IsEqualTo("user-a");
        await Assert.That(reloadedSession.CurrentRefreshTokenHash).IsEqualTo("refresh-2");
        await Assert.That(reloadedSession.RefreshGeneration).IsEqualTo(1);
    }

    [Test]
    public async Task SessionWithoutAnIssuedRefreshTokenCannotBeFoundByARefreshTokenHash()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        var session = ExternalAuthenticationTestData.CreateSession(_now);
        session.CurrentRefreshTokenHash = null!;
        await store.SaveAsync(session);

        var persisted = await store.FindByIdAsync(session.Id);
        var matched = await store.FindByRefreshTokenHashAsync(null!);

        await Assert.That(persisted!.CurrentRefreshTokenHash).IsNull();
        await Assert.That(matched).IsNull();
    }

    [Test]
    public async Task SavingASessionWithAnExistingRefreshTokenHashIsRejectedAcrossTenants()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        var first = ExternalAuthenticationTestData.CreateSession(_now);
        var second = ExternalAuthenticationTestData.CreateSession(_now);
        second.Id = "session-b";
        second.TenantId = "tenant-b";
        await store.SaveAsync(first);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.SaveAsync(second).AsTask());

        await Assert.That(await store.FindByIdAsync(second.Id)).IsNull();
        await Assert.That((await store.FindByRefreshTokenHashAsync(first.CurrentRefreshTokenHash!))!.Id).IsEqualTo(first.Id);
    }

    [Test]
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
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.SaveAsync(second).AsTask());

        var persistedFirst = await store.FindByIdAsync(first.Id);
        var persistedSecond = await store.FindByIdAsync(second.Id);
        await Assert.That(persistedFirst).IsNotNull();
        await Assert.That(persistedSecond).IsNotNull();
        var nonNullPersistedFirst = persistedFirst!;
        var nonNullPersistedSecond = persistedSecond!;
        await Assert.That(nonNullPersistedFirst.CurrentRefreshTokenHash).IsEqualTo(first.CurrentRefreshTokenHash);
        await Assert.That(nonNullPersistedSecond.CurrentRefreshTokenHash).IsNull();
        await Assert.That((await store.FindByRefreshTokenHashAsync(first.CurrentRefreshTokenHash!))!.Id).IsEqualTo(first.Id);
    }

    [Test]
    public async Task RotatingToAnExistingRefreshTokenHashFailsWithoutMutatingEitherSession()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        var first = ExternalAuthenticationTestData.CreateSession(_now);
        var second = ExternalAuthenticationTestData.CreateSession(_now);
        second.Id = "session-b";
        second.CurrentRefreshTokenHash = "refresh-2";
        await store.SaveAsync(first);
        await store.SaveAsync(second);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.TryRotateRefreshTokenAsync(first.Id, "refresh-1", 0, "refresh-2", _now.AddMinutes(1)).AsTask());

        var persistedFirst = await store.FindByIdAsync(first.Id);
        var persistedSecond = await store.FindByIdAsync(second.Id);
        await Assert.That(persistedFirst).IsNotNull();
        await Assert.That(persistedSecond).IsNotNull();
        var nonNullPersistedFirst = persistedFirst!;
        var nonNullPersistedSecond = persistedSecond!;
        await Assert.That(nonNullPersistedFirst.CurrentRefreshTokenHash).IsEqualTo("refresh-1");
        await Assert.That(nonNullPersistedFirst.RefreshGeneration).IsEqualTo(0);
        await Assert.That(nonNullPersistedFirst.LastRefreshedAt).IsEqualTo(_now);
        await Assert.That(nonNullPersistedSecond.CurrentRefreshTokenHash).IsEqualTo("refresh-2");
        await Assert.That((await store.FindByRefreshTokenHashAsync("refresh-2"))!.Id).IsEqualTo(second.Id);
    }

    [Test]
    public async Task ReusingASupersededRefreshTokenRevokesTheSession()
    {
        var clock = new TestSystemClock(_now);
        var store = new InMemoryExternalAuthenticationSessionStore(clock);
        await store.SaveAsync(ExternalAuthenticationTestData.CreateSession(_now));
        await store.TryRotateRefreshTokenAsync("session-a", "refresh-1", 0, "refresh-2", _now.AddMinutes(1));

        var replay = await store.TryRotateRefreshTokenAsync("session-a", "refresh-1", 0, "refresh-3", _now.AddMinutes(2));
        var currentTokenAttempt = await store.TryRotateRefreshTokenAsync("session-a", "refresh-2", 1, "refresh-3", _now.AddMinutes(2));
        var session = await store.FindByIdAsync("session-a");

        await Assert.That(replay).IsOfType(typeof(ExternalAuthenticationSessionRotationResult.Reused));
        await Assert.That(currentTokenAttempt).IsOfType(typeof(ExternalAuthenticationSessionRotationResult.Revoked));
        await Assert.That(session).IsNotNull();
        var revokedSession = session!;
        await Assert.That(revokedSession.RevokedAt).IsEqualTo(clock.UtcNow);
        await Assert.That(revokedSession.RevocationReason).IsEqualTo("refresh_token_reuse");
    }

    [Test]
    public async Task ExpiredSessionCannotBeRotated()
    {
        var clock = new TestSystemClock(_now);
        var session = ExternalAuthenticationTestData.CreateSession(_now);
        session.RefreshExpiresAt = _now.AddSeconds(1);
        var store = new InMemoryExternalAuthenticationSessionStore(clock);
        await store.SaveAsync(session);
        clock.UtcNow = _now.AddSeconds(1);

        var result = await store.TryRotateRefreshTokenAsync(session.Id, "refresh-1", 0, "refresh-2", clock.UtcNow);

        await Assert.That(result).IsOfType(typeof(ExternalAuthenticationSessionRotationResult.Expired));
    }

    [Test]
    public async Task ExplicitRevocationIsACompareAndSetOperation()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        await store.SaveAsync(ExternalAuthenticationTestData.CreateSession(_now));

        var first = await store.RevokeAsync("session-a", "administrator", _now.AddMinutes(1));
        var second = await store.RevokeAsync("session-a", "administrator", _now.AddMinutes(1));

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsFalse();
    }

    [Test]
    public async Task ConcurrentRefreshRotationsPermitOnlyOneWinner()
    {
        var store = new InMemoryExternalAuthenticationSessionStore(new TestSystemClock(_now));
        await store.SaveAsync(ExternalAuthenticationTestData.CreateSession(_now));

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index =>
            store.TryRotateRefreshTokenAsync("session-a", "refresh-1", 0, $"refresh-{index + 2}", _now.AddMinutes(1)).AsTask()));

        await Assert.That(results.OfType<ExternalAuthenticationSessionRotationResult.Rotated>()).HasSingleItem();
        await Assert.That(results).Contains(result => result is ExternalAuthenticationSessionRotationResult.Reused);
        var session = await store.FindByIdAsync("session-a");
        await Assert.That(session).IsNotNull();
        await Assert.That(session!.RevocationReason).IsEqualTo("refresh_token_reuse");
    }
}
