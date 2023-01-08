using Elsa.Elasticsearch.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Management;

public class WorkflowInstanceConfiguration : IElasticConfiguration
{
    public Type DocumentType() => typeof(WorkflowInstance);
    
    public void Apply(ConnectionSettings connectionSettings, IDictionary<string,string> aliasConfig)
    {
        connectionSettings
            .DefaultMappingFor<WorkflowInstance>(m => m
                .IndexName(aliasConfig[nameof(WorkflowInstance)])
                .Ignore(p => p.WorkflowState));
    }
}