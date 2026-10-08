namespace Elsa.Studio.Security.Models;

public enum IdentityPermissionSnapshotState
{
    Ready,
    Forbidden,
    Unavailable
}

/// <summary>
/// Effective concrete grants returned for the current access token.
/// </summary>
public sealed record IdentityPermissionSnapshot(
    IdentityPermissionSnapshotState State,
    IReadOnlyDictionary<string, IReadOnlySet<string>> Grants)
{
    private static IReadOnlyDictionary<string, IReadOnlySet<string>> EmptyGrants { get; } =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

    public static IdentityPermissionSnapshot Forbidden { get; } =
        new(IdentityPermissionSnapshotState.Forbidden, EmptyGrants);

    public static IdentityPermissionSnapshot Unavailable { get; } =
        new(IdentityPermissionSnapshotState.Unavailable, EmptyGrants);

    public bool HasPermission(string resource, string verb) =>
        State == IdentityPermissionSnapshotState.Ready &&
        Grants.TryGetValue(resource, out var verbs) &&
        verbs.Contains(verb);

}
