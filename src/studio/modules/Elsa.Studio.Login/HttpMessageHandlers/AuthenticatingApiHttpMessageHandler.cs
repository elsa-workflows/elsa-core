using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Elsa.Studio.Login.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that configures the outgoing HTTP request to use the access token as bearer token.
/// </summary>
public class AuthenticatingApiHttpMessageHandler(IRefreshTokenService refreshTokenService, IBlazorServiceAccessor blazorServiceAccessor)
    : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sp = blazorServiceAccessor.Services;
        var jwtAccessor = sp.GetRequiredService<IJwtAccessor>();
        var accessToken = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);

        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Refresh token and retry once.
            var tokens = await refreshTokenService.RefreshTokenAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(tokens.AccessToken))
                return response;

            request.Headers.Authorization = new("Bearer", tokens.AccessToken);

            // Retry.
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}
