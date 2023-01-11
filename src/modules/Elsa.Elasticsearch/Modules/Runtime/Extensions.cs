using Elsa.Workflows.Runtime.Features;

namespace Elsa.Elasticsearch.Modules.Runtime;

public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="ExecutionLogRecordFeature"/> to use the <see cref="ElasticExecutionLogRecordFeature"/>.
    /// </summary>
    public static ExecutionLogRecordFeature UseElasticsearch(this ExecutionLogRecordFeature feature, Action<ElasticExecutionLogRecordFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}