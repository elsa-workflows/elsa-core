using System.Net;
using System.Net.Http.Headers;

namespace Elsa.OrchardCore.Client;

public class AuthenticatingDelegatingHandler(ISecurityTokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenService.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token might be expired; refresh it
            token = await tokenService.RefreshTokenAsync(cancellationToken);

            // Retry the request with the new token
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}