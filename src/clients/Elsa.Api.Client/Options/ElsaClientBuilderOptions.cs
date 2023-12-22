using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.Client.Options;

/// <summary>
/// Represents options for building an Elsa API client.
/// </summary>
public class ElsaClientBuilderOptions
{
    /// <summary>
    /// Gets or sets a delegate that can be used to configure the HTTP client builder.
    /// </summary>
    public Action<IHttpClientBuilder>? ConfigureHttpClientBuilder { get; set; }
    
    
}