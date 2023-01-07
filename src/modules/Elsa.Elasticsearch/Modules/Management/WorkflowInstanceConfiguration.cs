using Elsa.Elasticsearch.Services;
using Elsa.Workflows.Management.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Management;

public class WorkflowInstanceConfiguration : IElasticConfiguration
{
    public void Apply(ConnectionSettings connectionSettings, IDictionary<string,string> aliasConfig)
    {
        connectionSettings.DefaultMappingFor<WorkflowInstance>(m => 
            m.IndexName(aliasConfig[nameof(WorkflowInstance)]));
    }
}