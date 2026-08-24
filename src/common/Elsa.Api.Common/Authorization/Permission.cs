namespace Elsa.Authorization;

/// <summary>
/// A permission: a hierarchical resource path paired with a verb, written <c>{resource}:{verb}</c>.
/// Both axes are open and string-keyed, and both accept a wildcard.
/// </summary>
/// <remarks>
/// A trailing <c>*</c> on the resource axis matches the named node and every descendant at any depth,
/// so <c>workflows/definitions/*</c> covers <c>workflows/definitions</c> itself as well as
/// <c>workflows/definitions/versions</c>. A bare <c>*</c> resource matches everything, and <c>*</c> as a
/// verb matches any verb. Wildcards are the only construct with forward reach: they cover resources and
/// verbs registered in later releases.
/// </remarks>
public readonly record struct Permission(string Resource, string Verb)
{
    /// <summary>Separates the resource from the verb.</summary>
    public const char Separator = ':';

    /// <summary>Separates resource path segments.</summary>
    public const char PathSeparator = '/';

    /// <summary>Matches any resource, or any verb, depending on the axis it appears on.</summary>
    public const string Wildcard = "*";

    /// <summary>The whole vocabulary. Superuser is this grant, not a special case in the evaluator.</summary>
    public static Permission All { get; } = new(Wildcard, Wildcard);

    /// <summary>Whether the resource axis is the bare wildcard.</summary>
    public bool IsResourceWildcard => Resource == Wildcard;

    /// <summary>Whether the verb axis is the wildcard.</summary>
    public bool IsVerbWildcard => Verb == Wildcard;

    /// <summary>Whether the resource names a subtree, as in <c>workflows/*</c>.</summary>
    public bool IsSubtree => Resource.Length > 2 && Resource.EndsWith($"{PathSeparator}{Wildcard}", StringComparison.Ordinal);

    /// <summary>Whether either axis carries a wildcard.</summary>
    public bool HasWildcard => IsResourceWildcard || IsVerbWildcard || IsSubtree;

    /// <summary>
    /// Parses <paramref name="value"/>, returning <c>false</c> when it is not a well-formed permission.
    /// </summary>
    /// <remarks>
    /// A bare <c>*</c> — no separator, the wildcard alone — normalizes to <see cref="All"/>. This is a
    /// parsing rule rather than an evaluation special case, so the evaluator never sees a sentinel. It is
    /// what lets a stored or seeded <c>*</c> keep authorizing across the vocabulary migration without a
    /// lock-out window.
    /// </remarks>
    public static bool TryParse(string? value, out Permission permission)
    {
        permission = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        if (trimmed == Wildcard)
        {
            permission = All;
            return true;
        }

        // A permission may never contain a comma: the persistence converter joins collections with one.
        if (trimmed.Contains(','))
            return false;

        var separator = trimmed.IndexOf(Separator);

        if (separator <= 0 || separator == trimmed.Length - 1)
            return false;

        var resource = trimmed[..separator];
        var verb = trimmed[(separator + 1)..];

        if (verb.IndexOf(Separator) >= 0 || verb.IndexOf(PathSeparator) >= 0)
            return false;

        permission = new(resource, verb);
        return true;
    }

    /// <summary>Parses <paramref name="value"/>, throwing when it is not a well-formed permission.</summary>
    public static Permission Parse(string value) =>
        TryParse(value, out var permission) ? permission : throw new FormatException($"'{value}' is not a well-formed permission. Expected '{{resource}}:{{verb}}'.");

    /// <inheritdoc />
    public override string ToString() => $"{Resource}{Separator}{Verb}";
}
