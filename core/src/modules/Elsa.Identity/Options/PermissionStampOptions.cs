namespace Elsa.Identity.Options;

/// <summary>Controls the optional permission stamp, which tightens revocation below the token lifetime.</summary>
public class PermissionStampOptions
{
    /// <summary>
    /// Whether tokens carry a permission stamp that is revalidated on each request. Off by default: the
    /// access-token lifetime already bounds revocation, and this trades a cached store read per user per
    /// interval for a tighter bound.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// How long a computed stamp is cached per node. This, not zero, is the effective revocation bound
    /// when the stamp is enabled. Each node computes independently from the store, so no cross-node cache
    /// invalidation is required.
    /// </summary>
    public TimeSpan CacheLifetime { get; set; } = TimeSpan.FromSeconds(30);
}
