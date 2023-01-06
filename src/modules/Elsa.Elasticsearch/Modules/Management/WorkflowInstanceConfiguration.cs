using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Workflows.Management.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Management;

public class WorkflowInstanceConfiguration : IElasticConfiguration
{
    private const string IndexName = "workflow-instance";
    
    public void Apply(ConnectionSettings connectionSettings, IDictionary<string,string> indexConfig)
    {
        connectionSettings.DefaultMappingFor<WorkflowInstance>(m => 
            m.IndexName(IElasticConfiguration.ResolveIndexName<WorkflowInstance>(indexConfig, IndexName)));
    }
}