namespace Elsa.Api.Client.Contracts;

/// <summary>
/// An interface for classes that can configure an HTTP request made to the API.
/// </summary>
public interface IApiHttpRequestConfigurator
{
    /// <summary>
    /// Configures the HTTP request.
    /// </summary>
    Task ConfigureRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}