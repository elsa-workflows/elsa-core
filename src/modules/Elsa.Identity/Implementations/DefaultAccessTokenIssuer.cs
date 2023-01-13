using System.Security.Claims;
using Elsa.Common.Services;
using Elsa.Identity.Entities;
using Elsa.Identity.Options;
using Elsa.Identity.Services;
using FastEndpoints.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.Implementations;

public class DefaultAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly ISystemClock _systemClock;
    private readonly IdentityTokenOptions _identityOptions;

    public DefaultAccessTokenIssuer(ISystemClock systemClock, IOptions<IdentityTokenOptions> identityOptions)
    {
        _systemClock = systemClock;
        _identityOptions = identityOptions.Value;
    }

    public ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default)
    {
        var permissions = user.Roles.SelectMany(x => x.Permissions).ToList();
        var (signingKey, issuer, audience, accessTokenLifetime, refreshTokenLifetime) = _identityOptions;

        if (string.IsNullOrWhiteSpace(signingKey)) throw new Exception("No signing key configured");
        if (string.IsNullOrWhiteSpace(issuer)) throw new Exception("No issuer configured");
        if (string.IsNullOrWhiteSpace(audience)) throw new Exception("No audience configured");

        var nameClaim = new Claim(JwtRegisteredClaimNames.Name, user.Name);
        var claims = new[] { nameClaim };

        var accessTokenExpiresAt = _systemClock.UtcNow.Add(accessTokenLifetime);
        var refreshTokenExpiresAt = _systemClock.UtcNow.Add(refreshTokenLifetime);
        var accessToken = JWTBearer.CreateToken(signingKey, accessTokenExpiresAt.UtcDateTime, permissions, issuer: issuer, audience: audience, claims: claims);
        var refreshToken = JWTBearer.CreateToken(signingKey, refreshTokenExpiresAt.UtcDateTime, permissions, issuer: issuer, audience: audience, claims: claims);

        return new (new IssuedTokens(accessToken, refreshToken));
    }
}

public record IssuedTokens(string AccessToken, string RefreshToken);