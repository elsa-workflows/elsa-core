namespace Elsa.Studio.Authorization;

/// <summary>
/// Decides whether a held permission satisfies a required one, using the same rules as the backend evaluator:
/// exact match, or a wildcard on either axis.
/// </summary>
public static class PermissionMatcher
{
    /// <summary>Whether <paramref name="granted"/> satisfies <paramref name="required"/>.</summary>
    public static bool Satisfies(Permission granted, Permission required) =>
        ResourceMatches(granted.Resource, required.Resource) && VerbMatches(granted.Verb, required.Verb);

    /// <summary>Whether any of <paramref name="granted"/> satisfies <paramref name="required"/>.</summary>
    public static bool Satisfies(IEnumerable<Permission> granted, Permission required) =>
        granted.Any(x => Satisfies(x, required));

    /// <summary>
    /// Whether a granted resource pattern covers a required resource. A trailing <c>/*</c> matches the named node
    /// and every descendant at any depth; a bare <c>*</c> matches everything.
    /// </summary>
    public static bool ResourceMatches(string granted, string required)
    {
        if (granted == Permission.Wildcard || string.Equals(granted, required, StringComparison.Ordinal))
            return true;

        if (!granted.EndsWith($"{Permission.PathSeparator}{Permission.Wildcard}", StringComparison.Ordinal))
            return false;

        // 'workflows/*' covers 'workflows' itself as well as everything beneath it.
        var prefix = granted[..^2];

        return string.Equals(prefix, required, StringComparison.Ordinal)
               || required.Length > prefix.Length
               && required.StartsWith(prefix, StringComparison.Ordinal)
               && required[prefix.Length] == Permission.PathSeparator;
    }

    /// <summary>Whether a granted verb covers a required verb.</summary>
    public static bool VerbMatches(string granted, string required) =>
        granted == Permission.Wildcard || string.Equals(granted, required, StringComparison.Ordinal);
}
