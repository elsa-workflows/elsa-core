using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Models;
using Elsa.Elasticsearch.Options;
using Elsa.Workflows.Management.Features;

namespace Elsa.Elasticsearch.Modules.Management;

public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowInstanceFeature"/> to use the <see cref="ElasticWorkflowInstanceFeature"/>.
    /// </summary>
    public static WorkflowInstanceFeature UseElasticsearch(
        this WorkflowInstanceFeature feature, 
        ElasticsearchOptions options,
        IndexRolloverStrategy? rolloverStrategy = default,
        IDictionary<string,string>? indexConfig = default,
        Action<ElasticWorkflowInstanceFeature>? configure = default)
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