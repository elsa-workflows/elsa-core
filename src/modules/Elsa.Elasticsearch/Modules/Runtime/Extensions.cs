using Elsa.Elasticsearch.Options;
using Elsa.Workflows.Runtime.Features;

namespace Elsa.Elasticsearch.Modules.Runtime;

public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="ExecutionLogRecordFeature"/> to use the <see cref="ElasticExecutionLogRecordFeature"/>.
    /// </summary>
    public static ExecutionLogRecordFeature UseElasticsearch(this ExecutionLogRecordFeature feature, ElasticsearchOptions options, Action<ElasticExecutionLogRecordFeature>? configure = default)
    {
        configure += f => f.Options = options;
        feature.Module.Configure(configure);
        return feature;
    }
}