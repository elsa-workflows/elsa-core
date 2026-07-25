using System.Security.Claims;
using Elsa.Common;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Identity.Services;

/// <summary>
/// Creates signed Elsa JWTs from trusted issuance data.
/// </summary>
public sealed class DefaultElsaTokenService(ISystemClock systemClock, IOptions<IdentityTokenOptions> identityTokenOptions) : IElsaTokenService
{
    /// <inheritdoc />
    public ValueTask<IssuedAccessToken> IssueAccessTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default)
    {
        return IssueTokenAsync(context, TokenUse.Access, identityTokenOptions.Value.AccessTokenLifetime, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<IssuedAccessToken> IssueRefreshTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default)
    {
        return IssueTokenAsync(context, TokenUse.Refresh, identityTokenOptions.Value.RefreshTokenLifetime, cancellationToken);
    }

    private ValueTask<IssuedAccessToken> IssueTokenAsync(
        TokenIssuanceContext context,
        string tokenUse,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tokenOptions = identityTokenOptions.Value;

        if (string.IsNullOrWhiteSpace(tokenOptions.SigningKey))
            throw new InvalidOperationException("No signing key configured");
        if (string.IsNullOrWhiteSpace(tokenOptions.Issuer))
            throw new InvalidOperationException("No issuer configured");
        if (string.IsNullOrWhiteSpace(tokenOptions.Audience))
            throw new InvalidOperationException("No audience configured");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, context.User.Name)
        };
        claims.AddRange(context.AdditionalClaims);

        if (!string.IsNullOrWhiteSpace(context.User.TenantId))
            claims.Add(new Claim(tokenOptions.TenantIdClaimsType, context.User.TenantId));

        if (!string.IsNullOrWhiteSpace(context.ExternalAuthenticationSessionId))
            claims.Add(new Claim(CustomClaimTypes.ExternalAuthenticationSessionId, context.ExternalAuthenticationSessionId));

        var expiresAt = systemClock.UtcNow.Add(lifetime);
        claims.AddRange(context.Roles.Select(x => new Claim(ClaimTypes.Role, x)));
        claims.AddRange(context.Permissions.Select(x => new Claim("permissions", x)));
        claims.Add(new Claim(TokenUse.ClaimType, tokenUse));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.UtcDateTime,
            Issuer = tokenOptions.Issuer,
            Audience = tokenOptions.Audience,
            SigningCredentials = new SigningCredentials(tokenOptions.CreateSecurityKey(), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return ValueTask.FromResult(new IssuedAccessToken(token, expiresAt));
    }
}
