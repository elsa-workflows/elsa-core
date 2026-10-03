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
    /// Creates a refresh token without a subject claim. Only 3.8.0-preview1 issued Elsa refresh tokens this way,
    /// and they had a 2-hour lifetime, so no Elsa-issued refresh token still in use lacks a <c>sub</c>. Refresh
    /// now fails closed for these.
    /// </summary>
    public static string CreateWithoutSubject(IdentityTokenOptions options, User user) => Create(options, user.Name, subject: null);

    /// <summary>
    /// Creates a refresh token with an explicit subject, including blank values used to prove a present empty
    /// <c>sub</c> is rejected rather than resolved by name.
    /// </summary>
    public static string CreateWithSubject(IdentityTokenOptions options, User user, string subject) => Create(options, user.Name, subject);

    private static string Create(IdentityTokenOptions options, string name, string? subject)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, name),
            new(TokenUse.ClaimType, TokenUse.Refresh)
        };

        if (subject is not null)
        {
            claims.Insert(0, new Claim(JwtRegisteredClaimNames.Sub, subject));
        }

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
