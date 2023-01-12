using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;

namespace Elsa.Elasticsearch.Modules.Management;

/// <summary>
/// Extends the <see cref="WorkflowInstanceFeature"/> feature.
/// </summary>
[PublicAPI]
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