using System.Security.Claims;
using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using FastEndpoints.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public class DefaultAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly IRoleProvider _roleProvider;
    private readonly ISystemClock _systemClock;
    private readonly IdentityTokenOptions _identityOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAccessTokenIssuer"/> class.
    /// </summary>
    public DefaultAccessTokenIssuer(IRoleProvider roleProvider, ISystemClock systemClock, IOptions<IdentityTokenOptions> identityOptions)
    {
        _roleProvider = roleProvider;
        _systemClock = systemClock;
        _identityOptions = identityOptions.Value;
    }

    /// <inheritdoc />
    public async ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = (await _roleProvider.FindByIdsAsync(user.Roles, cancellationToken)).ToList();
        var permissions = roles.SelectMany(x => x.Permissions).ToList();
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

        return new IssuedTokens(accessToken, refreshToken);
    }
}