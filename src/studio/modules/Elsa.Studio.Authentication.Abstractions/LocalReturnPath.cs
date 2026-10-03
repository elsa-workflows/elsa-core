namespace Elsa.Studio.Authentication.Abstractions;

/// <summary>
/// Neutralizes open-redirect targets so post-authentication navigation stays inside the Studio host.
/// </summary>
public static class LocalReturnPath
{
    private const int MaxDecodePasses = 8;

    /// <summary>
    /// Returns the original candidate when it is a local destination, or <c>/</c> when it is missing, absolute, protocol-relative, or otherwise unsafe.
    /// </summary>
    /// <remarks>
    /// Validation runs on a decoded copy. The returned value is the original string so encoded query values and path segments stay byte-for-byte intact.
    /// Scheme-less relative paths such as <c>workflows/x</c> are treated as local after prefixing <c>/</c> for the check only.
    /// Decoding that does not reach a fixed point within <see cref="MaxDecodePasses"/> fails closed to <c>/</c>.
    /// </remarks>
    public static string Normalize(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate))
        {
            return "/";
        }

        var decoded = candidate;
        for (var pass = 0; pass < MaxDecodePasses; pass++)
        {
            string next;
            try
            {
                next = Uri.UnescapeDataString(decoded);
            }
            catch (UriFormatException)
            {
                return "/";
            }

            if (next == decoded)
            {
                return IsLocalDestination(candidate) && IsLocalDestination(decoded) ? candidate : "/";
            }

            decoded = next;
        }

        return "/";
    }

    private static bool IsLocalDestination(string path)
    {
        if (IsRootedLocalPath(path))
        {
            return true;
        }

        if (path.Length == 0 || char.IsWhiteSpace(path[0]) || HasSchemeOrHost(path))
        {
            return false;
        }

        return IsRootedLocalPath("/" + path);
    }

    private static bool IsRootedLocalPath(string path) =>
        path.StartsWith('/') &&
        !path.StartsWith("//", StringComparison.Ordinal) &&
        !path.Any(c => c == '\\' || char.IsControl(c));

    private static bool HasSchemeOrHost(string path)
    {
        if (path.StartsWith("//", StringComparison.Ordinal) ||
            path.StartsWith('\\') ||
            path.Contains('\\'))
        {
            return true;
        }

        var schemeSeparator = path.IndexOf(':');
        if (schemeSeparator <= 0)
        {
            return false;
        }

        var queryStart = path.IndexOf('?');
        if (queryStart >= 0 && schemeSeparator > queryStart)
        {
            return false;
        }

        if (!char.IsAsciiLetter(path[0]))
        {
            return false;
        }

        for (var i = 1; i < schemeSeparator; i++)
        {
            var c = path[i];
            if (!char.IsAsciiLetterOrDigit(c) && c != '+' && c != '-' && c != '.')
            {
                return false;
            }
        }

        return true;
    }
}
