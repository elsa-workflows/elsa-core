using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Http.Features;
using Elsa.Http.OpenApi.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to install the HTTP OpenAPI feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Install the <see cref="HttpOpenApiFeature"/> feature as part of the HTTP feature configuration.
    /// </summary>
    public static HttpFeature UseOpenApi(this HttpFeature feature, Action<HttpOpenApiFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
