using Elsa.Api.Client.Options;
using Elsa.Studio.Options;

namespace Elsa.Studio.Models;

/// <summary>
/// Represents the backend api config.
/// </summary>
public class BackendApiConfig
{
    /// <summary>
    /// Gets or sets the configure http client builder action.
    /// </summary>
    public Action<ElsaClientBuilderOptions>? ConfigureHttpClientBuilder { get; set; }
    /// <summary>
    /// Gets or sets the configure backend options action.
    /// </summary>
    public Action<BackendOptions>? ConfigureBackendOptions { get; set; }
}