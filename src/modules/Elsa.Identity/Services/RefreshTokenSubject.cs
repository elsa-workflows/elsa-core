using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.Services;

/// <summary>
/// Reads the user id from an Elsa Identity refresh token.
/// </summary>
/// <remarks>
/// Claim order: <c>sub</c> (<see cref="JwtRegisteredClaimNames.Sub"/>) first, then the inbound-mapped
/// <see cref="ClaimTypes.NameIdentifier"/>. Elsa issues <c>sub</c>; JWT bearer maps that inbound claim to
/// NameIdentifier. Refresh resolves the user only by a non-blank subject. A missing or blank subject fails
/// closed. Only 3.8.0-preview1 issued refresh tokens without <c>sub</c>, and they had a 2-hour lifetime, so
/// no Elsa-issued refresh token still in use lacks a subject.
/// </remarks>
internal static class RefreshTokenSubject
{
    /// <summary>
    /// The token's user id, or <c>null</c> when neither subject claim is present or the present value is blank.
    /// </summary>
    public static string? FindUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(JwtRegisteredClaimNames.Sub) ?? principal.FindFirst(ClaimTypes.NameIdentifier);

        if (claim is null || string.IsNullOrWhiteSpace(claim.Value))
        {
            return null;
        }

        return claim.Value;
    }

    /// <inheritdoc cref="FindUserId(ClaimsPrincipal)"/>
    public static string? FindUserId(ClaimsIdentity identity) => FindUserId(new ClaimsPrincipal(identity));
}
