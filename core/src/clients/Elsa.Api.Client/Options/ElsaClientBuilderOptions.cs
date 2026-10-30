using System.Text.Json;
using Elsa.Api.Client.HttpMessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Polly;

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
    /// Gets or sets a delegate that can be used to configure the retry policy.
    /// </summary>
    public Action<IHttpClientBuilder>? ConfigureRetryPolicy { get; set; } = builder => builder.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

    /// <summary>
    /// Gets or sets a delegate that can be used to configure the JSON serializer options.
    /// </summary>
    public Action<IServiceProvider, JsonSerializerOptions>? ConfigureJsonSerializerOptions { get; set; }
}