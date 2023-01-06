using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Workflows.Management.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Management;

public class WorkflowInstanceConfiguration : IElasticConfiguration
{
    public void Apply(ConnectionSettings connectionSettings)
    {
        connectionSettings.DefaultMappingFor<WorkflowInstance>(m => 
            m.IndexName(ElasticsearchOptions.Indices[typeof(WorkflowInstance)]));
    }
}