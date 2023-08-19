using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.HttpMessageHandlers;

/// <summary>
/// An HTTP message handler that invokes <see cref="IApiHttpRequestConfigurator"/> instances before sending the request.
/// </summary>
public class ApiHttpMessageHandler : DelegatingHandler
{
    private readonly IEnumerable<IApiHttpRequestConfigurator> _httpClientConfigurators;

    /// <inheritdoc />
    public ApiHttpMessageHandler(IEnumerable<IApiHttpRequestConfigurator> httpClientConfigurators)
    {
        _httpClientConfigurators = httpClientConfigurators;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Invoke configurators.
        foreach (var httpClientConfigurator in _httpClientConfigurators)
            await httpClientConfigurator.ConfigureRequestAsync(request, cancellationToken);

        // Continue processing the request
        return await base.SendAsync(request, cancellationToken);
    }
}