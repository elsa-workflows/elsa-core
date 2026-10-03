namespace Elsa.Studio.Authentication.Abstractions;

/// <summary>
/// Neutralizes open-redirect targets so post-authentication navigation stays inside the Studio host.
/// </summary>
public static class LocalReturnPath
{
    private const int MaxDecodePasses = 8;

    /// <summary>
    /// Returns the original candidate when it is a rooted local path, or <c>/</c> when it is missing, relative, absolute, protocol-relative, or otherwise unsafe.
    /// </summary>
    /// <remarks>
    /// Validation runs on a decoded copy. The returned value is the original string so encoded query values and path segments stay byte-for-byte intact.
    /// Only a single leading <c>/</c> (not <c>//</c> or <c>/\</c>) is accepted. Decoding that does not reach a fixed point within <see cref="MaxDecodePasses"/> fails closed to <c>/</c>.
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
            var next = Uri.UnescapeDataString(decoded);
            if (next == decoded)
            {
                return IsRootedLocalPath(candidate) && IsRootedLocalPath(decoded) ? candidate : "/";
            }

            decoded = next;
        }

        return "/";
    }

    /// <summary>
    /// Roots a base-relative candidate against <paramref name="baseUri"/>'s path, then normalizes.
    /// </summary>
    public static string RootAgainstBase(string? candidate, string baseUri)
    {
        var basePath = new Uri(baseUri).AbsolutePath;
        if (!basePath.EndsWith('/'))
        {
            basePath += "/";
        }

        var rooted = candidate is { Length: > 0 } && candidate[0] != '/'
            ? basePath + candidate
            : candidate;
        return Normalize(rooted);
    }

    private static bool IsRootedLocalPath(string path) =>
        path.StartsWith('/') &&
        !path.StartsWith("//", StringComparison.Ordinal) &&
        !path.Any(c => c == '\\' || char.IsControl(c));
}
