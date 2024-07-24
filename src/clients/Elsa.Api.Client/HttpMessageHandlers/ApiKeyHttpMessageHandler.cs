using System.Net.Http.Headers;
using Elsa.Api.Client.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Api.Client.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that configures the outgoing HTTP request to use an API key as the authorization header.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ApiKeyHttpMessageHandler"/> class.
/// </remarks>
public class ApiKeyHttpMessageHandler(IOptions<ElsaClientOptions> options) : DelegatingHandler
{
    private readonly ElsaClientOptions _options = options.Value;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = _options.ApiKey;
        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);

        return await base.SendAsync(request, cancellationToken);
    }
}