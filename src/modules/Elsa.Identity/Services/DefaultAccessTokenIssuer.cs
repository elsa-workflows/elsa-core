using System.Security.Claims;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Framework.System;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using FastEndpoints.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.Services;

/// <summary>
/// Default implementation of <see cref="IAccessTokenIssuer"/>.
/// </summary>
public class DefaultAccessTokenIssuer(IRoleProvider roleProvider, ISystemClock systemClock, IOptions<IdentityTokenOptions> identityTokenOptions) : IAccessTokenIssuer
{
    /// <inheritdoc />
    public async ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = (await roleProvider.FindByIdsAsync(user.Roles, cancellationToken)).ToList();
        var permissions = roles.SelectMany(x => x.Permissions).ToList();
        var roleNames = roles.Select(x => x.Name).ToList();
        var tokenOptions = identityTokenOptions.Value;
        var signingKey = tokenOptions.SigningKey;
        var issuer = tokenOptions.Issuer;
        var audience = tokenOptions.Audience;
        var accessTokenLifetime = tokenOptions.AccessTokenLifetime;
        var refreshTokenLifetime = tokenOptions.RefreshTokenLifetime;

        if (string.IsNullOrWhiteSpace(signingKey)) throw new Exception("No signing key configured");
        if (string.IsNullOrWhiteSpace(issuer)) throw new Exception("No issuer configured");
        if (string.IsNullOrWhiteSpace(audience)) throw new Exception("No audience configured");

        var nameClaim = new Claim(JwtRegisteredClaimNames.Name, user.Name);
        var claims = new List<Claim>
        {
            nameClaim
        };

        if (!string.IsNullOrWhiteSpace(user.TenantId))
        {
            var tenantIdClaim = new Claim(tokenOptions.TenantIdClaimsType, user.TenantId);
            claims.Add(tenantIdClaim);
        }

        var now = systemClock.UtcNow;
        var accessTokenExpiresAt = now.Add(accessTokenLifetime);
        var refreshTokenExpiresAt = now.Add(refreshTokenLifetime);
        var accessToken = JwtBearer.CreateToken(options => ConfigureTokenOptions(options, accessTokenExpiresAt.UtcDateTime));
        var refreshToken = JwtBearer.CreateToken(options => ConfigureTokenOptions(options, refreshTokenExpiresAt.UtcDateTime));

        return new IssuedTokens(accessToken, refreshToken);

        void ConfigureTokenOptions(JwtCreationOptions options, DateTime expireAt)
        {
            options.SigningKey = signingKey;
            options.ExpireAt = expireAt;
            options.Issuer = issuer;
            options.Audience = audience;
            options.User.Claims.AddRange(claims);
            options.User.Permissions.AddRange(permissions);
            options.User.Roles.AddRange(roleNames);
        }
    }
}