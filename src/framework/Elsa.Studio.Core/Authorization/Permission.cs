namespace Elsa.Studio.Authorization;

/// <summary>
/// A permission: a hierarchical resource path paired with a verb, written <c>{resource}:{verb}</c>.
/// </summary>
/// <remarks>
/// Mirrors the backend's permission grammar so Studio evaluates grants exactly as the server does. A trailing
/// <c>/*</c> on the resource matches the named node and every descendant, a bare <c>*</c> resource matches
/// everything, and <c>*</c> as a verb matches any verb.
/// </remarks>
public readonly record struct Permission(string Resource, string Verb)
{
    /// <summary>Separates the resource from the verb.</summary>
    public const char Separator = ':';

    /// <summary>Separates resource path segments.</summary>
    public const char PathSeparator = '/';

    /// <summary>Matches any resource, or any verb, depending on the axis it appears on.</summary>
    public const string Wildcard = "*";

    /// <summary>The whole vocabulary.</summary>
    public static Permission All { get; } = new(Wildcard, Wildcard);

    /// <summary>
    /// Parses <paramref name="value"/>, returning <c>false</c> when it is not a well-formed permission. A bare
    /// <c>*</c> normalizes to <see cref="All"/>.
    /// </summary>
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

    /// <inheritdoc />
    public override string ToString() => $"{Resource}{Separator}{Verb}";
}
