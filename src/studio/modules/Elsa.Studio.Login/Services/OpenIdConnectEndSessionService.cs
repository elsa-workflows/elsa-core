using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using System.Net;

namespace Elsa.Studio.Login.Services;

///<inheritdoc/>
public class OpenIdConnectEndSessionService(IOptions<OpenIdConnectConfiguration> options, IJwtAccessor jwtAccessor, NavigationManager navigationManager) : IEndSessionService
{
    ///<inheritdoc/>
    public async Task LogoutAsync()
    {
        await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, "");
        await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, "");

        string logoutUrl = "/";
        if (options.Value.EndSessionEndpoint is { } endSessionEndpoint)
        {
            logoutUrl = endSessionEndpoint + $"?post_logout_redirect_uri={WebUtility.UrlEncode(new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Authority))}";
            if (await jwtAccessor.ReadTokenAsync(TokenNames.IdToken) is { } idToken && !String.IsNullOrWhiteSpace(idToken))
            {
                logoutUrl += $"&id_token_hint={WebUtility.UrlEncode(idToken)}";
            }
        }

        await jwtAccessor.WriteTokenAsync(TokenNames.IdToken, "");
        navigationManager.NavigateTo(logoutUrl, true);
    }
}
