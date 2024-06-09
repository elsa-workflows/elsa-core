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
    public DefaultAccessTokenIssuer(IRoleProvider roleProvider, ISystemClock systemClock, IOptions<IdentityTokenOptions> identityTokenOptions)
    {
        _roleProvider = roleProvider;
        _systemClock = systemClock;
        _identityOptions = identityTokenOptions.Value;
    }

    /// <inheritdoc />
    public async ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = (await _roleProvider.FindByIdsAsync(user.Roles, cancellationToken)).ToList();
        var permissions = roles.SelectMany(x => x.Permissions).ToList();
        var signingKey = _identityOptions.SigningKey;
        var issuer = _identityOptions.Issuer;
        var audience = _identityOptions.Audience;
        var accessTokenLifetime = _identityOptions.AccessTokenLifetime;
        var refreshTokenLifetime = _identityOptions.RefreshTokenLifetime;

        if (string.IsNullOrWhiteSpace(signingKey)) throw new Exception("No signing key configured");
        if (string.IsNullOrWhiteSpace(issuer)) throw new Exception("No issuer configured");
        if (string.IsNullOrWhiteSpace(audience)) throw new Exception("No audience configured");

        var nameClaim = new Claim(JwtRegisteredClaimNames.Name, user.Name);
        var claims = new List<Claim> { nameClaim };
        
        if (!string.IsNullOrWhiteSpace(user.TenantId))
        {
            var tenantIdClaim = new Claim(_identityOptions.TenantIdClaimsType, user.TenantId);
            claims.Add(tenantIdClaim);
        }

        var accessTokenExpiresAt = _systemClock.UtcNow.Add(accessTokenLifetime);
        var refreshTokenExpiresAt = _systemClock.UtcNow.Add(refreshTokenLifetime);
        
        var accessToken = JwtBearer.CreateToken(options =>
        {
            options.SigningKey = signingKey;
            options.Issuer = issuer;
            options.Audience = audience;
            options.ExpireAt = accessTokenExpiresAt.UtcDateTime;
            options.User.Claims.AddRange(claims);
            options.User.Permissions.AddRange(permissions);
        });
        var refreshToken = JwtBearer.CreateToken(options =>
        {
            options.SigningKey = signingKey;
            options.Issuer = issuer;
            options.Audience = audience;
            options.ExpireAt = refreshTokenExpiresAt.UtcDateTime;
            options.User.Claims.AddRange(claims);
            options.User.Permissions.AddRange(permissions);
        });

        return new IssuedTokens(accessToken, refreshToken);
    }
}