using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Services;

/// <summary>Starts broker login through the Server controller so PKCE and confidential client state stay server-side.</summary>
public sealed class ServerExternalAuthenticationLoginCoordinator(
    IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
    ExternalAuthenticationClientOptions options,
    NavigationManager navigationManager) : ExternalAuthenticationLoginCoordinator(anonymousBackendApiClientProvider, options)
{
    public override string? LocalLoginAction => "/authentication/external/local-login";

    public override Task BeginExternalAsync(LoginMethodDescriptor method, string returnPath, CancellationToken cancellationToken = default)
    {
        var path = $"/authentication/external/login/{Uri.EscapeDataString(method.Key)}";
        navigationManager.NavigateTo(QueryHelpers.AddQueryString(path, "returnPath", LocalReturnPath.Normalize(returnPath)), forceLoad: true);
        return Task.CompletedTask;
    }

    public override Task BeginLocalAsync(string username, string password, string returnPath, CancellationToken cancellationToken = default)
    {
        // Local credentials must travel in a POST body and are never placed in the browser address bar.
        navigationManager.NavigateTo(QueryHelpers.AddQueryString("/authentication/external/local-login", "returnPath", LocalReturnPath.Normalize(returnPath)), forceLoad: true);
        return Task.CompletedTask;
    }
}
