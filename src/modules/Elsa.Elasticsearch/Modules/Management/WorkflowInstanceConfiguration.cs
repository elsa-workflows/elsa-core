using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Elasticsearch.Modules.Management;

public class WorkflowInstanceConfiguration : ElasticConfiguration<WorkflowInstance>
{
    public override void Apply(ElasticsearchClientSettings settings, IDictionary<Type,string> indexConfig)
    {
        settings
            .DefaultMappingFor<WorkflowInstance>(m => m
                .IndexName(indexConfig[typeof(WorkflowInstance)]));
    }
}