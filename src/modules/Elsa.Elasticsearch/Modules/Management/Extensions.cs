using Elsa.Workflows.Management.Features;

namespace Elsa.Elasticsearch.Modules.Management;

public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowInstanceFeature"/> to use the <see cref="ElasticWorkflowInstanceFeature"/>.
    /// </summary>
    public static WorkflowInstanceFeature UseElasticsearch(this WorkflowInstanceFeature feature, Action<ElasticWorkflowInstanceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}