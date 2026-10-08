using System.Security.Claims;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Models;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// Provides the authentication state for the current user based on a JWT token.
/// </summary>
public class AccessTokenAuthenticationStateProvider(
    IJwtAccessor jwtAccessor,
    IJwtParser jwtParser,
    IOptions<IdentityTokenOptions> options)
    : AuthenticationStateProvider
{
    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);

        if (string.IsNullOrWhiteSpace(token))
            return new(new(new ClaimsIdentity()));

        var claims = jwtParser.Parse(token).ToList();

        if (claims.IsExpired())
            return new(new(new ClaimsIdentity()));

        var identity = new ClaimsIdentity(claims, "jwt", options.Value.NameClaimType, options.Value.RoleClaimType);
        var user = new ClaimsPrincipal(identity);

        return new(user);
    }

    /// <summary>
    /// Notifies the authentication state has changed.
    /// </summary>
    public void NotifyAuthenticationStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
