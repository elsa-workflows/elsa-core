using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Elsa.Identity.Services;

/// <summary>
/// Validates and exchanges Elsa identity refresh tokens.
/// </summary>
public sealed class DefaultIdentityRefreshTokenService(
    IUserProvider userProvider,
    IAccessTokenIssuer accessTokenIssuer,
    ITenantAccessor tenantAccessor,
    IOptions<IdentityTokenOptions> identityTokenOptions) : IIdentityRefreshTokenService
{
    /// <inheritdoc />
    public async ValueTask<IssuedTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var options = identityTokenOptions.Value;
        var validationResult = await new JsonWebTokenHandler().ValidateTokenAsync(refreshToken, options.CreateTokenValidationParameters());

        if (!validationResult.IsValid)
            return null;

        var identity = validationResult.ClaimsIdentity;
        var tokenUse = identity.FindFirst(TokenUse.ClaimType)?.Value;

        if (!string.Equals(tokenUse, TokenUse.Refresh, StringComparison.Ordinal))
            return null;

        var userId = RefreshTokenSubject.FindUserId(identity);

        if (userId is null)
        {
            return null;
        }

        var tenantId = identity.FindFirst(options.TenantIdClaimsType)?.Value;
        var tenant = string.IsNullOrWhiteSpace(tenantId) ? null : new Tenant { Id = tenantId, Name = tenantId };
        using var tenantContext = tenantAccessor.PushContext(tenant);
        var user = await userProvider.FindByIdAsync(userId, cancellationToken);

        return user is null ? null : await accessTokenIssuer.IssueTokensAsync(user, cancellationToken);
    }
}
