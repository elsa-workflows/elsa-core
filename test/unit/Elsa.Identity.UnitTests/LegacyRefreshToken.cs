using System.Security.Claims;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Identity.UnitTests;

/// <summary>
/// Creates a refresh token the way Elsa issued them before sessions existed: without a session claim.
/// </summary>
internal static class LegacyRefreshToken
{
    public static string Create(IdentityTokenOptions options, User user) => Create(options, user, includeSubject: true);

    /// <summary>
    /// Creates a refresh token the way Elsa issued them before the subject claim was present.
    /// </summary>
    public static string CreateWithoutSubject(IdentityTokenOptions options, User user) => Create(options, user, includeSubject: false);

    private static string Create(IdentityTokenOptions options, User user, bool includeSubject)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(TokenUse.ClaimType, TokenUse.Refresh)
        };

        if (includeSubject)
        {
            claims.Insert(0, new Claim(JwtRegisteredClaimNames.Sub, user.Id));
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
