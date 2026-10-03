using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.Services;

/// <summary>
/// Reads the user id from an Elsa Identity refresh token.
/// </summary>
/// <remarks>
/// Collects every <c>sub</c> (<see cref="JwtRegisteredClaimNames.Sub"/>) and inbound-mapped
/// <see cref="ClaimTypes.NameIdentifier"/> value. Refresh resolves the user only when those values
/// agree on one non-blank id. None, any blank, or more than one distinct value fails closed. The JWT
/// bearer handler maps inbound <c>sub</c> to NameIdentifier and takes the first match, while
/// <c>JsonWebTokenHandler</c> leaves <c>sub</c> unmapped; looking at every value keeps both paths
/// aligned. 3.0–3.7 also issued refresh tokens without <c>sub</c>, but the refresh scheme already
/// rejects those because they lack <c>token_use</c>. 3.8.0-preview1 is the only release whose
/// refresh-scheme-accepted tokens lacked <c>sub</c>, and they had a 2-hour lifetime. Lookups are by
/// id only; that is safe under tenant-scoped stores because user ids are globally unique.
/// </remarks>
internal static class RefreshTokenSubject
{
    /// <summary>
    /// The token's user id, or <c>null</c> when no subject claim is present, any subject is blank, or
    /// the subject claims do not all agree.
    /// </summary>
    public static string? FindUserId(ClaimsPrincipal principal)
    {
        var values = principal.FindAll(JwtRegisteredClaimNames.Sub)
            .Concat(principal.FindAll(ClaimTypes.NameIdentifier))
            .Select(claim => claim.Value)
            .ToArray();

        if (values.Length == 0 || values.Any(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        var distinct = values.Distinct(StringComparer.Ordinal).ToArray();
        return distinct.Length == 1 ? distinct[0] : null;
    }

    /// <inheritdoc cref="FindUserId(ClaimsPrincipal)"/>
    public static string? FindUserId(ClaimsIdentity identity) => FindUserId(new ClaimsPrincipal(identity));
}
