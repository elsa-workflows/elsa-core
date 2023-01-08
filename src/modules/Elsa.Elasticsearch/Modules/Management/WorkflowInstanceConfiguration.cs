using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Management.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Management;

public class WorkflowInstanceConfiguration : ElasticConfiguration<WorkflowInstance>
{
    public override void Apply(ConnectionSettings connectionSettings, IDictionary<string,string> aliasConfig)
    {
        connectionSettings
            .DefaultMappingFor<WorkflowInstance>(m => m
                .IndexName(aliasConfig[nameof(WorkflowInstance)])
                .Ignore(p => p.WorkflowState));
    }
}