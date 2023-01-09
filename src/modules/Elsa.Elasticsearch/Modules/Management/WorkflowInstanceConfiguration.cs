using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Management.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Management;

public class WorkflowInstanceConfiguration : ElasticConfiguration<WorkflowInstance>
{
    public override void Apply(ConnectionSettings connectionSettings, IDictionary<Type,string> indexConfig)
    {
        connectionSettings
            .DefaultMappingFor<WorkflowInstance>(m => m
                .IndexName(indexConfig[typeof(WorkflowInstance)])
                .Ignore(p => p.WorkflowState));
    }
}