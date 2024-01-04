using Elsa.Api.Client.HttpMessageHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.Client.Options;

/// <summary>
/// Represents options for building an Elsa API client.
/// </summary>
public class ElsaClientBuilderOptions
{
    /// <summary>
    /// Gets or sets the base address of the Elsa server.
    /// </summary>
    public Uri BaseAddress { get; set; } = default!;

    /// <summary>
    /// Gets or sets the API key function to use when authenticating with the Elsa server.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// A <see cref="DelegatingHandler"/> type that can be used to authenticate with the Elsa server.
    /// Defaults to <see cref="ApiKeyHttpMessageHandler"/>.
    /// </summary>
    public Type AuthenticationHandler { get; set; } = typeof(ApiKeyHttpMessageHandler);

    /// <summary>
    /// Gets or sets a delegate that can be used to configure the HTTP client.
    /// </summary>
    public Action<IServiceProvider, HttpClient>? ConfigureHttpClient { get; set; }

    /// <summary>
    /// Gets or sets a delegate that can be used to configure the HTTP client builder.
    /// </summary>
    public Action<IHttpClientBuilder> ConfigureHttpClientBuilder { get; set; } = _ => { };

    /// <summary>
    /// Number of automatic retries for transient failures, including following categories:
    /// <list type = "bullet" >
    /// <item><description> Network failures(as <see cref = "HttpRequestException" />)</description></item>
    /// <item><description>HTTP 5XX status codes(server errors)</description></item>
    /// <item><description>HTTP 408 status code(request timeout)</description></item>
    /// </list>
    /// Set the value to 0 to disable automatic retry.
    /// </summary>
    public int TransientHttpErrorRetryCount { get; set; } = 3;
}