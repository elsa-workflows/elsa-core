using System.Security.Claims;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Identity.UnitTests;

/// <summary>
/// Creates refresh tokens in the shapes Elsa used to issue, for compatibility tests.
/// </summary>
internal static class LegacyRefreshToken
{
    /// <summary>
    /// Creates a refresh token the way Elsa issued them before the subject claim was present.
    /// </summary>
    public static string CreateWithoutSubject(IdentityTokenOptions options, User user)
    {
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new([
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(TokenUse.ClaimType, TokenUse.Refresh)
            ]),
            Expires = DateTime.UtcNow.Add(options.RefreshTokenLifetime),
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = new(options.CreateSecurityKey(), SecurityAlgorithms.HmacSha256Signature)
        });
    }
}
