using System.Security.Claims;
using Elsa.Common.Services;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Elsa.Identity.Services;

namespace Elsa.Identity.UnitTests.Services;

public class SessionRevokerTests
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromHours(2);
    private readonly MutableSystemClock _clock = new();
    private readonly MemoryStore<RevokedSession> _store = new();
    private readonly SessionRevoker _revoker;

    public SessionRevokerTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions { RefreshTokenLifetime = RefreshTokenLifetime });
        _revoker = new(new MemoryRevokedSessionStore(_store), _clock, options);
    }

    [Fact]
    public async Task RevokingASessionLeavesOtherSessionsLive()
    {
        Assert.False(await _revoker.IsRevokedAsync("session-a"));

        await _revoker.RevokeAsync("session-a", _clock.UtcNow);

        Assert.True(await _revoker.IsRevokedAsync("session-a"));
        Assert.False(await _revoker.IsRevokedAsync("session-b"));
    }

    [Fact]
    public async Task RevokingASessionTwiceIsHarmless()
    {
        await _revoker.RevokeAsync("session-a", _clock.UtcNow);
        await _revoker.RevokeAsync("session-a", _clock.UtcNow);

        Assert.True(await _revoker.IsRevokedAsync("session-a"));
        Assert.Single(_store.List());
    }

    [Fact]
    public async Task RevocationOutlivesARefreshTokenIssuedNow()
    {
        await _revoker.RevokeAsync("session-a", _clock.UtcNow.AddMinutes(1));

        Assert.True(Revocation("session-a").ExpiresAt > _clock.UtcNow + RefreshTokenLifetime);
    }

    [Fact]
    public async Task RevocationOutlivesThePresentedRefreshToken()
    {
        // A refresh token issued under a longer lifetime than the one configured now.
        var presentedExpiry = _clock.UtcNow + RefreshTokenLifetime * 3;

        await _revoker.RevokeAsync("session-a", presentedExpiry);

        Assert.True(Revocation("session-a").ExpiresAt > presentedExpiry);
    }

    [Fact]
    public async Task RevokingPrunesExpiredRevocationsOnly()
    {
        await _revoker.RevokeAsync("expired", _clock.UtcNow);
        _clock.UtcNow += TimeSpan.FromDays(1);
        await _revoker.RevokeAsync("live", _clock.UtcNow);
        _clock.UtcNow += RefreshTokenLifetime;

        await _revoker.RevokeAsync("latest", _clock.UtcNow);

        Assert.False(await _revoker.IsRevokedAsync("expired"));
        Assert.True(await _revoker.IsRevokedAsync("live"));
        Assert.True(await _revoker.IsRevokedAsync("latest"));
    }

    [Fact]
    public void SessionIdIsReadFromTheRefreshToken()
    {
        var identity = new ClaimsIdentity([new Claim(CustomClaimTypes.SessionId, "session-a")]);

        Assert.Equal("session-a", SessionRevoker.GetSessionId(identity, "token-a"));
    }

    [Fact]
    public void RefreshTokenWithoutSessionGetsOneDerivedFromTheToken()
    {
        var identity = new ClaimsIdentity();

        var sessionId = SessionRevoker.GetSessionId(identity, "token-a");

        Assert.Equal(sessionId, SessionRevoker.GetSessionId(identity, "token-a"));
        Assert.NotEqual(sessionId, SessionRevoker.GetSessionId(identity, "token-b"));
        Assert.DoesNotContain("token-a", sessionId, StringComparison.Ordinal);
    }

    private RevokedSession Revocation(string sessionId) => Assert.Single(_store.List(), x => x.Id == sessionId);
}
