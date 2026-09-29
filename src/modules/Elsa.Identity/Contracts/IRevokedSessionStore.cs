using Elsa.Identity.Entities;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Stores revoked sign-in sessions.
/// </summary>
public interface IRevokedSessionStore
{
    /// <summary>
    /// Adds or replaces the specified revocation.
    /// </summary>
    Task SaveAsync(RevokedSession revokedSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the session with the specified ID has been revoked.
    /// </summary>
    Task<bool> ExistsAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the revocations that expired before <paramref name="now"/>.
    /// </summary>
    Task DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
