namespace Elsa.Studio.Contracts;

/// <summary>
/// Clears a cached permission snapshot so the next resolution reloads grants for the current principal and backend.
/// </summary>
public interface IPermissionSnapshotCache
{
    /// <summary>Drops any cached snapshot, including Forbidden and Unavailable.</summary>
    void Invalidate();
}
