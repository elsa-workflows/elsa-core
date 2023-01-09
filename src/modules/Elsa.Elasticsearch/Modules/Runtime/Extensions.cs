using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Elasticsearch.Modules.Runtime;

public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="ExecutionLogRecordFeature"/> to use the <see cref="ElasticExecutionLogRecordFeature"/>.
    /// </summary>
    public static ExecutionLogRecordFeature UseElasticsearch(
        this ExecutionLogRecordFeature feature, 
        ElasticsearchOptions options, 
        IndexRolloverStrategy? rolloverStrategy = default,
        IDictionary<string,string>? indexConfig = default,
        Action<ElasticExecutionLogRecordFeature>? configure = default)
    {
        configure += f =>
        {
            f.Options = options;
            f.IndexRolloverStrategy = rolloverStrategy;
            f.IndexConfig = Utils.ResolveAliasConfig(f.IndexConfig, options.IndexConfig, indexConfig);
        };
        
        feature.Module.Configure(configure);
        return feature;
    }
}