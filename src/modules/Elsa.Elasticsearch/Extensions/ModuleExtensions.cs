using Elsa.Elasticsearch.Features;
using Elsa.Elasticsearch.Implementations.RolloverStrategies;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Features.Services;

namespace Elsa.Elasticsearch.Extensions;

public static class ModuleExtensions
{
    /// <summary>
    /// Enables the <see cref="ElasticsearchFeature"/> feature.
    /// </summary>
    public static IModule UseElasticsearch(
        this IModule module, 
        Action<ElasticsearchOptions> options,
        Func<IServiceProvider, IIndexRolloverStrategy>? rolloverStrategy = default,
        Action<ElasticsearchFeature>? configure = default)
    {
        configure += f =>
        {
            f.Options += options;
            f.IndexRolloverStrategy = rolloverStrategy ?? (_ => new NoRollover()) ;
        };
        
        module.Configure(configure);
        return module;
    }
}