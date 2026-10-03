using System.Text;

namespace Elsa.Studio.Authentication.Abstractions;

/// <summary>
/// Neutralizes open-redirect targets so post-authentication navigation stays inside the Studio host.
/// Shared with #8585 so logout (#1081) and return-path hardening compose on one helper.
/// </summary>
public static class LocalReturnPath
{
    /// <summary>
    /// Returns a rooted local path, or <c>/</c> when the candidate is missing, absolute, protocol-relative, or otherwise unsafe.
    /// </summary>
    public static string Normalize(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return "/";
        }

        var sanitized = DecodeAndStrip(candidate);
        if (!IsSafeLocalPath(sanitized))
        {
            return "/";
        }

        return sanitized;
    }

    private static bool IsSafeLocalPath(string candidate)
    {
        if (string.IsNullOrEmpty(candidate) ||
            !candidate.StartsWith("/", StringComparison.Ordinal) ||
            candidate.StartsWith("//", StringComparison.Ordinal) ||
            candidate.Contains('\\') ||
            candidate.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        return Uri.TryCreate(candidate, UriKind.Relative, out var uri) && !uri.IsAbsoluteUri;
    }

    private static string DecodeAndStrip(string candidate)
    {
        var current = candidate.Trim();
        for (var i = 0; i < 8; i++)
        {
            string decoded;
            try
            {
                decoded = Uri.UnescapeDataString(current);
            }
            catch (UriFormatException)
            {
                return string.Empty;
            }

            if (decoded == current)
            {
                break;
            }

            current = decoded;
        }

        return StripControlCharacters(current.Trim());
    }

    private static string StripControlCharacters(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}
