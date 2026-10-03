namespace Elsa.Studio.Contracts;

/// <summary>
/// Clears a cached permission snapshot so the next resolution reloads grants for the current principal and backend.
/// </summary>
public interface IPermissionSnapshotCache
{
    /// <summary>
    /// Raised after <see cref="Invalidate"/> so permission-dependent UI can re-fetch.
    /// Subscribers run after the snapshot is dropped, so a re-resolve cannot read the previous grants.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>Drops any cached snapshot, including Forbidden and Unavailable, then raises <see cref="Changed"/>.</summary>
    void Invalidate();
}
