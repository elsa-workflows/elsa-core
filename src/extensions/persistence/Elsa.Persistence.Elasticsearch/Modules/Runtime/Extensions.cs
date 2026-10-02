using Elsa.Workflows.Runtime.Features;

namespace Elsa.Persistence.Elasticsearch.Modules.Runtime;

/// <summary>
/// Provides extensions to the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowRuntimeFeature"/> to use the <see cref="ElasticExecutionLogRecordFeature"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseElasticsearch(this WorkflowRuntimeFeature feature, Action<ElasticExecutionLogRecordFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}