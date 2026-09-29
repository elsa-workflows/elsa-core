using Elsa.Common.Entities;

namespace Elsa.Identity.Entities;

/// <summary>
/// A sign-in session whose refresh tokens are no longer accepted. The <see cref="Entity.Id"/> is the session ID.
/// </summary>
/// <remarks>
/// Revocations are stored tenant-agnostic. They are checked while the refresh token is being authenticated,
/// before any tenant has been resolved, and session IDs are unique across tenants.
/// </remarks>
public class RevokedSession : Entity
{
    /// <summary>
    /// When the session was revoked.
    /// </summary>
    public DateTimeOffset RevokedAt { get; set; }

    /// <summary>
    /// When the last refresh token of the session expires, after which the revocation can be forgotten.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
