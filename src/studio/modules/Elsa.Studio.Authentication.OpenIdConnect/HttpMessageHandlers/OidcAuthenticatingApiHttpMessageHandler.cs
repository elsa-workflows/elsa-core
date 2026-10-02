using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that attaches an OIDC access token to outgoing HTTP requests.
/// </summary>
/// <remarks>
/// This handler uses the configured <see cref="OidcOptions.BackendApiScopes"/> to request tokens
/// for backend API calls in multi-audience scenarios. If no backend API scopes are configured,
/// it uses the default authentication scopes.
/// </remarks>
public class OidcAuthenticatingApiHttpMessageHandler(IBlazorServiceAccessor blazorServiceAccessor) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sp = blazorServiceAccessor.Services;
        var tokenProvider = sp.GetRequiredService<ITokenProvider>();
        var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
