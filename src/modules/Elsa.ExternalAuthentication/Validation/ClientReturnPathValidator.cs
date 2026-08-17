namespace Elsa.ExternalAuthentication.Validation;

/// <summary>
/// Validates a browser return path supplied to the authentication broker.
/// Only client-local absolute paths may be used to prevent open redirects.
/// </summary>
public static class ClientReturnPathValidator
{
    public const string DefaultReturnPath = "/";
    public const int MaximumLength = 2048;

    public static bool TryValidate(string? returnPath, out string validatedReturnPath)
    {
        validatedReturnPath = DefaultReturnPath;

        if (string.IsNullOrWhiteSpace(returnPath) || returnPath.Length > MaximumLength)
            return false;

        var candidate = returnPath;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            if (!IsClientLocalPath(candidate))
                return false;

            try
            {
                var decoded = Uri.UnescapeDataString(candidate);
                if (decoded == candidate)
                    break;

                candidate = decoded;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        if (!IsClientLocalPath(candidate))
            return false;

        validatedReturnPath = returnPath;
        return true;
    }

    public static string GetSafeReturnPath(string? returnPath) => TryValidate(returnPath, out var validatedReturnPath) ? validatedReturnPath : DefaultReturnPath;

    public static bool TryValidateForClient(string? returnPath, IReadOnlySet<string> allowedPrefixes, out string validatedReturnPath)
    {
        ArgumentNullException.ThrowIfNull(allowedPrefixes);

        if (!TryValidate(returnPath, out validatedReturnPath))
            return false;

        var path = StripQueryAndFragment(validatedReturnPath);
        if (allowedPrefixes.Any(prefix => IsAllowedPrefix(path, prefix)))
            return true;

        validatedReturnPath = DefaultReturnPath;
        return false;
    }

    private static bool IsClientLocalPath(string value)
    {
        return value.StartsWith("/", StringComparison.Ordinal)
            && !value.StartsWith("//", StringComparison.Ordinal)
            && value.IndexOf('\\') < 0
            && !value.Any(char.IsControl);
    }

    private static bool IsAllowedPrefix(string path, string prefix)
    {
        if (!IsClientLocalPath(prefix) || prefix.IndexOfAny(['?', '#']) >= 0)
            return false;

        var normalizedPrefix = prefix.Length > 1 ? prefix.TrimEnd('/') : prefix;
        return normalizedPrefix == "/"
            || string.Equals(path, normalizedPrefix, StringComparison.Ordinal)
            || path.StartsWith($"{normalizedPrefix}/", StringComparison.Ordinal);
    }

    private static string StripQueryAndFragment(string value)
    {
        var separatorIndex = value.IndexOfAny(['?', '#']);
        return separatorIndex < 0 ? value : value[..separatorIndex];
    }
}
