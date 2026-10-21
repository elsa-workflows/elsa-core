using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that attaches an ElsaIdentity JWT access token to outgoing HTTP requests.
/// </summary>
/// <remarks>
/// This handler retrieves the JWT access token from the authentication provider and automatically
/// handles token refresh when the token is expired or near expiry.
/// </remarks>
public class ElsaIdentityAuthenticatingApiHttpMessageHandler(IBlazorServiceAccessor blazorServiceAccessor) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sp = blazorServiceAccessor.Services;
        var authenticationProvider = sp.GetService<ITokenProvider>();
        var requestContextBinding = sp.GetService<IBackendApiClientProvider>() as IBackendRequestContextBinding;
        requestContextBinding?.RegisterAuthenticatedRequestTransport(authenticationProvider as IBackendRequestContextEpochSource);

        if (authenticationProvider == null)
        {
            requestContextBinding?.ValidateBoundRequestContext();
            return await base.SendAsync(request, cancellationToken);
        }

        // Get access token (with automatic refresh if needed).
        var accessToken = await authenticationProvider.GetAccessTokenAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
            requestContextBinding?.RegisterAuthenticatedRequestTransport(null);

        // Token acquisition can observe a changed identity or no usable token. Validate only
        // after that callback has completed and before attaching its token or dispatching the request.
        requestContextBinding?.ValidateBoundRequestContext();

        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
