using Elsa.Elasticsearch.Options;
using Elsa.Workflows.Management.Features;

namespace Elsa.Elasticsearch.Modules.Management;

public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowInstanceFeature"/> to use the <see cref="ElasticWorkflowInstanceFeature"/>.
    /// </summary>
    public static WorkflowInstanceFeature UseElasticsearch(this WorkflowInstanceFeature feature, ElasticsearchOptions options, Action<ElasticWorkflowInstanceFeature>? configure = default)
    {
        configure += f => f.Options = options;
        feature.Module.Configure(configure);
        return feature;
    }
}