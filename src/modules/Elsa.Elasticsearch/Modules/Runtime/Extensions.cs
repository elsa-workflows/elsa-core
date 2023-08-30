using Elsa.Workflows.Runtime.Features;

namespace Elsa.Elasticsearch.Modules.Runtime;

/// <summary>
/// Provides extensions to the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowRuntimeFeature"/> to use the <see cref="ElasticExecutionLogRecordFeature"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseElasticsearch(this WorkflowRuntimeFeature feature, Action<ElasticExecutionLogRecordFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}