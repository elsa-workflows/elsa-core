using Elsa.Elasticsearch.Features;
using Elsa.Elasticsearch.Options;
using Elsa.Features.Services;
using JetBrains.Annotations;

namespace Elsa.Elasticsearch.Extensions;

/// <summary>
/// Extends <see cref="IModule"/> to configure the <see cref="ElasticsearchFeature"/> feature.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    /// <summary>
    /// Enables the <see cref="ElasticsearchFeature"/> feature.
    /// </summary>
    public static IModule UseElasticsearch(
        this IModule module, 
        Action<ElasticsearchOptions> options,
        Action<ElasticsearchFeature>? configure = default)
    {
        configure += f => f.Options += options;
        module.Configure(configure);
        return module;
    }
}