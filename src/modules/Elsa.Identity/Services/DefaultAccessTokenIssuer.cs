using Elsa.Common;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.Services;

/// <summary>
/// Default implementation of <see cref="IAccessTokenIssuer"/>.
/// </summary>
public class DefaultAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly IRoleProvider _roleProvider;
    private readonly IElsaTokenService _tokenService;
    private readonly IPermissionStampCalculator? _stampCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAccessTokenIssuer"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DefaultAccessTokenIssuer(IRoleProvider roleProvider, IElsaTokenService tokenService, IPermissionStampCalculator? stampCalculator = null)
    {
        _roleProvider = roleProvider;
        _tokenService = tokenService;
        _stampCalculator = stampCalculator;
    }

    /// <summary>
    /// Initializes a new instance using the legacy constructor shape.
    /// </summary>
    public DefaultAccessTokenIssuer(IRoleProvider roleProvider, ISystemClock systemClock, IOptions<IdentityTokenOptions> identityTokenOptions)
        : this(roleProvider, new DefaultElsaTokenService(systemClock, identityTokenOptions))
    {
    }

    /// <inheritdoc />
    public async ValueTask<IssuedTokens> IssueTokensAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = (await _roleProvider.FindByIdsAsync(user.Roles, cancellationToken)).ToList();
        var permissions = roles.SelectMany(x => x.Permissions).ToList();
        var roleNames = roles.Select(x => x.Name).ToList();

        // The stamp is derived from the same roles that produced these permissions, so it changes exactly
        // when the grants do. Issued unconditionally; only validation is gated by configuration, which
        // means turning the feature on does not invalidate tokens already in flight.
        var stamp = _stampCalculator is null ? null : await _stampCalculator.ComputeAsync(user, cancellationToken);
        var additionalClaims = stamp is null
            ? Array.Empty<System.Security.Claims.Claim>()
            : [new System.Security.Claims.Claim(PermissionStampCalculator.ClaimType, stamp)];

        var context = new TokenIssuanceContext(user, roleNames, permissions, additionalClaims);
        var accessToken = await _tokenService.IssueAccessTokenAsync(context, cancellationToken);
        var refreshToken = await _tokenService.IssueRefreshTokenAsync(context, cancellationToken);

        return new IssuedTokens(accessToken.Token, refreshToken.Token);
    }
}
