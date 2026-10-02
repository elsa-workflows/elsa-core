namespace Elsa.Studio.Security.Models;

/// <summary>The kind of grant represented by a stored role permission.</summary>
public enum RolePermissionGrantKind
{
    Exact,
    Wildcard,
    Unresolved
}

/// <summary>A parsed permission pattern used by the role authoring surface.</summary>
public readonly record struct RolePermissionPattern(string Resource, string Verb)
{
    public bool IsResourceWildcard => Resource == "*";
    public bool IsVerbWildcard => Verb == "*";
    public bool IsSubtree => Resource.Length > 2 && Resource.EndsWith("/*", StringComparison.Ordinal);
    public bool HasWildcard => IsResourceWildcard || IsVerbWildcard || IsSubtree;

    public override string ToString() => $"{Resource}:{Verb}";
}

/// <summary>Classifies stored grants against the current Identity permission catalog.</summary>
public static class RolePermissionAuthoring
{
    /// <summary>
    /// Trims, ordinally de-duplicates, and ordinally sorts grants before sending them to Core.
    /// Null remains an empty collection because an editor always sends the complete current grant set.
    /// </summary>
    public static IReadOnlyList<string> NormalizePermissions(IEnumerable<string>? permissions) =>
        (permissions ?? [])
            .Where(x => x != null)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Parses the Core permission grammar without rejecting unfamiliar stored values.</summary>
    public static bool TryParse(string? value, out RolePermissionPattern pattern)
    {
        pattern = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed == "*")
        {
            pattern = new("*", "*");
            return true;
        }

        if (trimmed.Contains(',', StringComparison.Ordinal))
            return false;

        var separator = trimmed.IndexOf(':');
        if (separator <= 0 || separator == trimmed.Length - 1)
            return false;

        var resource = trimmed[..separator];
        var verb = trimmed[(separator + 1)..];
        if (verb.Contains(':', StringComparison.Ordinal) || verb.Contains('/', StringComparison.Ordinal))
            return false;

        pattern = new(resource, verb);
        return true;
    }

    /// <summary>Whether a parsed pattern is valid according to Core's wildcard placement rules.</summary>
    public static bool IsValidPattern(RolePermissionPattern pattern)
    {
        if (pattern.Verb != "*" && pattern.Verb.Contains('*', StringComparison.Ordinal))
            return false;

        var first = pattern.Resource.IndexOf('*');
        return first < 0 || (first == pattern.Resource.LastIndexOf('*') && (pattern.IsResourceWildcard || pattern.IsSubtree));
    }

    /// <summary>Whether a wildcard grant matches a concrete catalog resource and verb.</summary>
    public static bool Matches(RolePermissionPattern grant, string resource, string verb)
    {
        var resourceMatches = grant.IsResourceWildcard ||
                              string.Equals(grant.Resource, resource, StringComparison.Ordinal) ||
                              (grant.IsSubtree &&
                               (string.Equals(grant.Resource[..^2], resource, StringComparison.Ordinal) ||
                                resource.StartsWith(grant.Resource[..^1], StringComparison.Ordinal)));
        var verbMatches = grant.IsVerbWildcard || string.Equals(grant.Verb, verb, StringComparison.Ordinal);
        return resourceMatches && verbMatches;
    }

    /// <summary>Determines whether a stored value is a recognized catalog or wildcard grant.</summary>
    public static bool IsRecognized(
        string? value,
        IEnumerable<PermissionResourceDescriptor> catalog,
        out RolePermissionGrantKind kind)
    {
        kind = RolePermissionGrantKind.Unresolved;
        if (!TryParse(value, out var pattern) || !IsValidPattern(pattern))
            return false;

        if (pattern.HasWildcard)
        {
            kind = RolePermissionGrantKind.Wildcard;
            return true;
        }

        var descriptor = catalog.FirstOrDefault(x => string.Equals(x.Resource, pattern.Resource, StringComparison.Ordinal));
        if (descriptor == null || !descriptor.SupportedVerbs.Contains(pattern.Verb, StringComparer.Ordinal))
            return false;

        kind = RolePermissionGrantKind.Exact;
        return true;
    }

    /// <summary>Returns whether a concrete catalog permission is already covered by a stored wildcard.</summary>
    public static bool IsCoveredByWildcard(
        string resource,
        string verb,
        IEnumerable<string> wildcardGrants) =>
        wildcardGrants.Any(grant => TryParse(grant, out var pattern) &&
                                    pattern.HasWildcard &&
                                    IsValidPattern(pattern) &&
                                    Matches(pattern, resource, verb));

    /// <summary>Gets concrete grants for the currently displayed catalog descriptors.</summary>
    public static IReadOnlyList<string> GetConcreteGrants(IEnumerable<PermissionResourceDescriptor> descriptors) =>
        descriptors
            .SelectMany(resource => resource.SupportedVerbs.Select(verb => $"{resource.Resource}:{verb}"))
            .ToArray();

    /// <summary>Gets the resources represented by a wildcard's reach response, preserving its verb pattern.</summary>
    public static IReadOnlyList<string> GetReachedPermissions(RolePermissionPattern pattern, PermissionReachResponse? reach)
    {
        if (reach == null)
            return [];

        return reach.Covers
            .Select(resource => $"{resource}:{pattern.Verb}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }
}
