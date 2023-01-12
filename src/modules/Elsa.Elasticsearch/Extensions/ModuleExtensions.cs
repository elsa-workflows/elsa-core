using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Features;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Features.Services;

namespace Elsa.Elasticsearch.Extensions;

public static class ModuleExtensions
{
    /// <summary>
    /// Enables the <see cref="ElasticsearchFeature"/> feature.
    /// </summary>
    public static IModule UseElasticsearch(
        this IModule module, 
        ElasticsearchOptions options,
        IndexRolloverStrategy? rolloverStrategy = default,
        IDictionary<string,string>? indexConfig = default,
        Action<ElasticsearchFeature>? configure = default)
    {
        configure += f =>
        {
            f.Options = options;
            f.IndexRolloverStrategy = rolloverStrategy;
            f.IndexConfig = Utils.ResolveIndexConfig(f.IndexConfig, options.IndexConfig ?? indexConfig);
        };
        
        module.Configure(configure);
        return module;
    }
}