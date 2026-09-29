using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;

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
    /// The ID of the session <paramref name="refreshToken"/> belongs to. A refresh token issued before sessions
    /// existed carries none, so it gets one derived from the token itself, which refreshing then carries over.
    /// </summary>
    /// <param name="refreshTokenIdentity">The identity validated from <paramref name="refreshToken"/>.</param>
    /// <param name="refreshToken">The serialized refresh token.</param>
    public static string GetSessionId(ClaimsIdentity refreshTokenIdentity, string refreshToken)
    {
        var sessionId = refreshTokenIdentity.FindFirst(CustomClaimTypes.SessionId)?.Value;

        return string.IsNullOrWhiteSpace(sessionId)
            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)))
            : sessionId;
    }

    /// <summary>
    /// Whether the session with the specified ID has been revoked.
    /// </summary>
    public async ValueTask<bool> IsRevokedAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await store.ExistsAsync(sessionId, cancellationToken);
    }

    /// <summary>
    /// Revokes the session with the specified ID. Revoking a session twice is harmless.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="refreshTokenExpiresAt">When the refresh token that identified the session expires.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask RevokeAsync(string sessionId, DateTimeOffset refreshTokenExpiresAt, CancellationToken cancellationToken = default)
    {
        var now = systemClock.UtcNow;

        // No refresh token of the session can outlive both the one presented and one issued right now, and
        // none can be issued after this point. Beyond that, the revocation guards nothing.
        var newestExpiry = now + identityTokenOptions.Value.RefreshTokenLifetime;
        var expiresAt = (refreshTokenExpiresAt > newestExpiry ? refreshTokenExpiresAt : newestExpiry) + ExpiryMargin;

        await store.DeleteExpiredAsync(now, cancellationToken);
        await store.SaveAsync(new()
        {
            Id = sessionId,
            TenantId = Tenant.AgnosticTenantId,
            RevokedAt = now,
            ExpiresAt = expiresAt
        }, cancellationToken);
    }
}
