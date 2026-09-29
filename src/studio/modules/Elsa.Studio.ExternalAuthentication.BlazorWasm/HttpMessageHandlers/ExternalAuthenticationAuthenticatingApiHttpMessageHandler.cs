using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.HttpMessageHandlers;

/// <summary>Attaches a current broker-issued Elsa access token to authenticated Studio backend requests.</summary>
public sealed class ExternalAuthenticationAuthenticatingApiHttpMessageHandler(IBlazorServiceAccessor blazorServiceAccessor) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokenProvider = blazorServiceAccessor.Services.GetRequiredService<IExternalAuthenticationTokenProvider>();
        var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
