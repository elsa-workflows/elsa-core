using Elsa.Studio.Extensions;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// Provides the authentication state for the current user based on an access token.
/// </summary>
public class AccessTokenAuthenticationStateProvider(IJwtAccessor jwtAccessor, IJwtParser jwtParser, IOptions<IdentityTokenOptions> options) : AuthenticationStateProvider
{
    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var authToken = await jwtAccessor.ReadTokenAsync(TokenNames.IdToken)
                     ?? await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);

        if (string.IsNullOrEmpty(authToken))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = jwtParser.Parse(authToken).ToList();
        var isExpired = claims.IsExpired();

        // If the token has expired, return an empty authentication state.
        if (isExpired)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        // Otherwise, return the authentication state.
        var identity = new ClaimsIdentity(claims, "jwt", options.Value.NameClaimType, options.Value.RoleClaimType);
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    /// <summary>
    /// Notifies the authentication state has changed.
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}