namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Centralizes removal of values that must never leave the external-authentication trust boundary.
/// </summary>
public static class ExternalAuthenticationRedactor
{
    public const string RedactedValue = "[REDACTED]";

    public static string RedactSecret(string? secret) => RedactedValue;

    public static string RedactToken(string? token) => RedactedValue;

    public static string RedactProviderResponseBody(string? responseBody) => RedactedValue;

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> RedactRawClaims(IReadOnlyDictionary<string, IReadOnlyCollection<string>>? claims)
    {
        return new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> RedactProjectedClaims(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> claims,
        IReadOnlySet<string> redactedClaimTypes)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentNullException.ThrowIfNull(redactedClaimTypes);

        var redactedClaims = new Dictionary<string, IReadOnlyCollection<string>>(claims.Count, StringComparer.Ordinal);
        foreach (var (claimType, values) in claims)
            redactedClaims[claimType] = redactedClaimTypes.Contains(claimType) ? [RedactedValue] : values.ToArray();

        return redactedClaims;
    }
}
