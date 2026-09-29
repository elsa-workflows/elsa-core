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
    public static string Create(IdentityTokenOptions options, User user) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Subject = new([
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(TokenUse.ClaimType, TokenUse.Refresh)
        ]),
        Expires = DateTime.UtcNow.Add(options.RefreshTokenLifetime),
        Issuer = options.Issuer,
        Audience = options.Audience,
        SigningCredentials = new(options.CreateSecurityKey(), SecurityAlgorithms.HmacSha256Signature)
    });
}
