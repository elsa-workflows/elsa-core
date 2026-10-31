using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// OpenID Connect sign-out events for providers that don't advertise an end_session_endpoint.
/// </summary>
internal static class OidcSignOutEvents
{
    /// <summary>
    /// Without an end_session_endpoint the handler throws instead of redirecting, failing the sign-out request.
    /// Ends the local session only.
    /// </summary>
    public static Task RedirectToIdentityProviderForSignOut(RedirectContext context)
    {
        if (string.IsNullOrEmpty(context.ProtocolMessage.IssuerAddress))
        {
            context.Response.Redirect(context.Properties.RedirectUri ?? "/");
            context.HandleResponse();
        }

        return Task.CompletedTask;
    }
}
