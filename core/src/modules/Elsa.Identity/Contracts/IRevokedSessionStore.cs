using Elsa.Identity.Entities;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Stores revoked sign-in sessions.
/// </summary>
public interface IRevokedSessionStore
{
    /// <summary>
    /// Adds the specified revocation or, when the session is already revoked, extends that revocation to the later
    /// <see cref="RevokedSession.ExpiresAt"/> of the two. A revocation's expiry never decreases, including when two
    /// revocations of the same session are written concurrently.
    /// </summary>
    Task AddOrExtendAsync(RevokedSession revokedSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the session with the specified ID has been revoked.
    /// </summary>
    Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the revocations that expired before <paramref name="now"/>.
    /// </summary>
    Task DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
