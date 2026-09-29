using System.Security.Claims;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Contains extension methods for <see cref="IEnumerable{Claim}" />.
/// </summary>
public static class ClaimsExtensions
{
    /// <summary>
    /// Determines whether the "exp" claim is expired.
    /// </summary>
    public static bool IsExpired(this IEnumerable<Claim> claims)
    {
        var expString = claims.FirstOrDefault(x => x.Type == "exp")?.Value.Trim();
        var exp = !string.IsNullOrEmpty(expString) ? long.Parse(expString) : 0;
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);
        return expiresAt < DateTimeOffset.UtcNow;
    }
}