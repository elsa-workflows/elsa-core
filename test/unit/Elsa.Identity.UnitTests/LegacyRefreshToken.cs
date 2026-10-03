using System.Security.Claims;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Identity.UnitTests;

/// <summary>
/// Creates refresh tokens in the shapes Elsa used to issue, for compatibility and rejection tests.
/// </summary>
internal static class LegacyRefreshToken
{
    /// <summary>
    /// Creates a refresh token the way Elsa issued them before sessions existed: with a subject, without a session claim.
    /// </summary>
    public static string Create(IdentityTokenOptions options, User user) => Create(options, user.Name, user.Id);

    /// <summary>
    /// Creates a refresh token without a subject claim. 3.0–3.7 also issued refresh tokens this way, but the
    /// refresh scheme already rejects those because they lack <c>token_use</c>. 3.8.0-preview1 is the only
    /// release whose refresh-scheme-accepted tokens lacked <c>sub</c>, and they had a 2-hour lifetime.
    /// Refresh now fails closed for these.
    /// </summary>
    public static string CreateWithoutSubject(IdentityTokenOptions options, User user) => Create(options, user.Name, subject: null);

    /// <summary>
    /// Creates a refresh token with an explicit subject, including blank values used to prove a present empty
    /// <c>sub</c> is rejected rather than resolved by name.
    /// </summary>
    public static string CreateWithSubject(IdentityTokenOptions options, User user, string subject) => Create(options, user.Name, subject);

    /// <summary>
    /// Creates a refresh token with the given subject claims, in the order supplied, so tests can pin
    /// conflicting <c>sub</c> / <see cref="ClaimTypes.NameIdentifier"/> combinations.
    /// </summary>
    public static string CreateWithSubjectClaims(IdentityTokenOptions options, User user, params Claim[] subjectClaims) =>
        Create(options, user.Name, subjectClaims);

    private static string Create(IdentityTokenOptions options, string name, string? subject)
    {
        Claim[] subjectClaims = subject is null ? [] : [new Claim(JwtRegisteredClaimNames.Sub, subject)];
        return Create(options, name, subjectClaims);
    }

    private static string Create(IdentityTokenOptions options, string name, IReadOnlyList<Claim> subjectClaims)
    {
        var claims = new List<Claim>(subjectClaims)
        {
            new(JwtRegisteredClaimNames.Name, name),
            new(TokenUse.ClaimType, TokenUse.Refresh)
        };

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new(claims),
            Expires = DateTime.UtcNow.Add(options.RefreshTokenLifetime),
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = new(options.CreateSecurityKey(), SecurityAlgorithms.HmacSha256Signature)
        });
    }
}
