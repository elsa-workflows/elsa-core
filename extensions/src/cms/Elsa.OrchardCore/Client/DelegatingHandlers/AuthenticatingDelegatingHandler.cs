using System.Net;
using System.Net.Http.Headers;
using Elsa.OrchardCore.Client.Contracts;

namespace Elsa.OrchardCore.Client.DelegatingHandlers;

/// <summary>
/// A delegating handler responsible for managing authentication by adding a bearer token
/// to outgoing HTTP requests. It retrieves and refreshes tokens using the provided
/// <see cref="ISecurityTokenService"/> to ensure authenticated communication with external systems.
/// </summary>
public class AuthenticatingDelegatingHandler(ISecurityTokenService tokenService) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenService.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new("Bearer", token.AccessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token might be expired; refresh it
            token = await tokenService.RefreshTokenAsync(cancellationToken);

            // Retry the request with the new token
            request.Headers.Authorization = new("Bearer", token.AccessToken);
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}