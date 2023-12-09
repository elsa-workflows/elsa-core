namespace Elsa.Api.Client.Options;

/// <summary>
/// Represents options for the Elsa client.
/// </summary>
public class ElsaClientOptions
{
    /// <summary>
    /// Gets or sets the base address of the Elsa server.
    /// </summary>
    public Uri BaseAddress { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the API key to use when authenticating with the Elsa server.
    /// </summary>
    public string ApiKey { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets a delegate that can be used to configure the HTTP client.
    /// </summary>
    public Action<IServiceProvider, HttpClient>? ConfigureHttpClient { get; set; }
}