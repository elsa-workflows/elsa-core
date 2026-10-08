using System.Security.Claims;
using Elsa.Common.Services;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using Microsoft.IdentityModel.JsonWebTokens;

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

        await RevokeAsync("session-a", _clock.UtcNow);

        Assert.True(await _revoker.IsRevokedAsync("session-a"));
        Assert.False(await _revoker.IsRevokedAsync("session-b"));
    }

    [Fact]
    public async Task RevokingASessionTwiceIsHarmless()
    {
        await RevokeAsync("session-a", _clock.UtcNow);
        await RevokeAsync("session-a", _clock.UtcNow);

        Assert.True(await _revoker.IsRevokedAsync("session-a"));
        Assert.Single(_store.List());
    }

    [Fact]
    public async Task RevocationOutlivesARefreshTokenIssuedNow()
    {
        await RevokeAsync("session-a", _clock.UtcNow.AddMinutes(1));

        Assert.True(Revocation("session-a").ExpiresAt > _clock.UtcNow + RefreshTokenLifetime);
    }

    [Fact]
    public async Task RevocationOutlivesEveryRefreshTokenTheSessionWentThrough()
    {
        // One of them was issued with a longer lifetime than the one configured now.
        var sessionExpiresAt = _clock.UtcNow + RefreshTokenLifetime * 3;

        await RevokeAsync("session-a", sessionExpiresAt);

        Assert.True(Revocation("session-a").ExpiresAt > sessionExpiresAt);
    }

    [Fact]
    public async Task RevokingASessionAgainNeverShortensItsRevocation()
    {
        await RevokeAsync("session-a", _clock.UtcNow + RefreshTokenLifetime * 3);
        var expiresAt = Revocation("session-a").ExpiresAt;

        await RevokeAsync("session-a", _clock.UtcNow);

        Assert.Equal(expiresAt, Revocation("session-a").ExpiresAt);
    }

    [Fact]
    public async Task RevokingASessionAgainExtendsItsRevocation()
    {
        await RevokeAsync("session-a", _clock.UtcNow);
        var revokedAt = Revocation("session-a").RevokedAt;
        var sessionExpiresAt = _clock.UtcNow + RefreshTokenLifetime * 3;
        _clock.UtcNow += TimeSpan.FromMinutes(1);

        await RevokeAsync("session-a", sessionExpiresAt);

        Assert.True(Revocation("session-a").ExpiresAt > sessionExpiresAt);
        Assert.Equal(revokedAt, Revocation("session-a").RevokedAt);
    }

    [Fact]
    public async Task RevokingPrunesExpiredRevocationsOnly()
    {
        await RevokeAsync("expired", _clock.UtcNow);
        _clock.UtcNow += TimeSpan.FromDays(1);
        await RevokeAsync("live", _clock.UtcNow);
        _clock.UtcNow += RefreshTokenLifetime;

        await RevokeAsync("latest", _clock.UtcNow);

        Assert.False(await _revoker.IsRevokedAsync("expired"));
        Assert.True(await _revoker.IsRevokedAsync("live"));
        Assert.True(await _revoker.IsRevokedAsync("latest"));
    }

    [Fact]
    public void SessionIsReadFromTheRefreshToken()
    {
        var identity = Identity((CustomClaimTypes.SessionId, "session-a"), (CustomClaimTypes.SessionExpiresAt, "2000000000"), (JwtRegisteredClaimNames.Exp, "1900000000"));

        var expected = new SignInSession("session-a", DateTimeOffset.FromUnixTimeSeconds(2000000000));
        Assert.Equal(expected, SessionRevoker.GetSession(identity, "token-a"));
        Assert.Equal(expected, SessionRevoker.FindSession(identity));
    }

    [Fact]
    public void SessionExpiresNoEarlierThanTheRefreshTokenItIsReadFrom()
    {
        var identity = Identity((CustomClaimTypes.SessionId, "session-a"), (CustomClaimTypes.SessionExpiresAt, "1900000000"), (JwtRegisteredClaimNames.Exp, "2000000000"));

        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(2000000000), SessionRevoker.GetSession(identity, "token-a").ExpiresAt);
    }

    [Fact]
    public void RefreshTokenWithoutSessionGetsOneDerivedFromTheTokenThatExpiresWithIt()
    {
        var identity = Identity((JwtRegisteredClaimNames.Exp, "2000000000"));

        var session = SessionRevoker.GetSession(identity, "token-a");

        Assert.Equal(session, SessionRevoker.GetSession(identity, "token-a"));
        Assert.NotEqual(session.Id, SessionRevoker.GetSession(identity, "token-b").Id);
        Assert.DoesNotContain("token-a", session.Id, StringComparison.Ordinal);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(2000000000), session.ExpiresAt);
    }

    [Fact]
    public void AnIdentityWithoutSessionNamesNone()
    {
        Assert.Null(SessionRevoker.FindSession(Identity((JwtRegisteredClaimNames.Exp, "2000000000"))));
    }

    private ValueTask RevokeAsync(string sessionId, DateTimeOffset sessionExpiresAt) => _revoker.RevokeAsync(new(sessionId, sessionExpiresAt));

    private RevokedSession Revocation(string sessionId) => Assert.Single(_store.List(), x => x.Id == sessionId);

    private static ClaimsIdentity Identity(params (string Type, string Value)[] claims) => new(claims.Select(x => new Claim(x.Type, x.Value)));
}
