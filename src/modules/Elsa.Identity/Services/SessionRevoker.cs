using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.Services;

/// <summary>
/// Revokes sign-in sessions, and tells whether a refresh token belongs to a revoked one.
/// </summary>
/// <remarks>
/// Only refresh tokens are revoked. Access tokens stay valid until they expire, which
/// <see cref="IdentityTokenOptions.AccessTokenLifetime"/> keeps short.
/// </remarks>
public class SessionRevoker(IRevokedSessionStore store, ISystemClock systemClock, IOptions<IdentityTokenOptions> identityTokenOptions)
{
    // A refresh racing the revocation can issue a token a moment after it, and nodes' clocks differ.
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The session <paramref name="refreshToken"/> belongs to. A refresh token issued before sessions existed carries
    /// none, so it gets one derived from the token itself, which refreshing then carries over, and that expires with it.
    /// </summary>
    /// <param name="refreshTokenIdentity">The identity validated from <paramref name="refreshToken"/>.</param>
    /// <param name="refreshToken">The serialized refresh token.</param>
    public static SignInSession GetSession(ClaimsIdentity refreshTokenIdentity, string refreshToken)
    {
        return FindSession(refreshTokenIdentity)
               ?? new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))), GetSessionExpiresAt(refreshTokenIdentity));
    }

    /// <summary>
    /// The session named by the specified identity. The refresh-token scheme puts it on the identity it authenticates,
    /// including the session derived for a refresh token issued before sessions existed. <c>null</c> when there is
    /// none, as with a custom refresh-token scheme, whose tokens are not Elsa Identity's to revoke.
    /// </summary>
    public static SignInSession? FindSession(ClaimsIdentity refreshTokenIdentity)
    {
        var sessionId = refreshTokenIdentity.FindFirst(CustomClaimTypes.SessionId)?.Value;

        return string.IsNullOrWhiteSpace(sessionId) ? null : new(sessionId, GetSessionExpiresAt(refreshTokenIdentity));
    }

    /// <summary>
    /// Whether the session with the specified ID has been revoked.
    /// </summary>
    public async ValueTask<bool> IsRevokedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await store.ExistsAsync(sessionId, cancellationToken);
    }

    /// <summary>
    /// Revokes the specified session. Revoking a session again can extend its revocation, never shorten it.
    /// </summary>
    /// <param name="session">The session, as read from one of its refresh tokens.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask RevokeAsync(SignInSession session, CancellationToken cancellationToken = default)
    {
        var now = systemClock.UtcNow;

        // The refresh tokens the session went through expire by session.ExpiresAt, whatever lifetime they were issued
        // with, and a refresh racing this revocation can still issue one a lifetime from now. None can be issued after
        // this point, so beyond the later of the two the revocation guards nothing. The store keeps an earlier
        // revocation of the session that expires later still.
        var newestExpiry = now + identityTokenOptions.Value.RefreshTokenLifetime;
        var expiresAt = (session.ExpiresAt > newestExpiry ? session.ExpiresAt : newestExpiry) + ExpiryMargin;

        await store.DeleteExpiredAsync(now, cancellationToken);
        await store.AddOrExtendAsync(new()
        {
            Id = session.Id,
            TenantId = Tenant.AgnosticTenantId,
            RevokedAt = now,
            ExpiresAt = expiresAt
        }, cancellationToken);
    }

    // A refresh token issued before sessions carried their expiry knows no expiry of its session later than its own.
    private static DateTimeOffset GetSessionExpiresAt(ClaimsIdentity refreshTokenIdentity)
    {
        return new[] { CustomClaimTypes.SessionExpiresAt, JwtRegisteredClaimNames.Exp }
            .Select(x => long.TryParse(refreshTokenIdentity.FindFirst(x)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : DateTimeOffset.MinValue)
            .Max();
    }
}
