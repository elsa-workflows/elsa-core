using Elsa.Elasticsearch.Services;
using Elsa.Workflows.Runtime.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Runtime;

public class ExecutionLogConfiguration : IElasticConfiguration
{
    private const string IndexName = "workflow-execution-log";
    
    public void Apply(ConnectionSettings connectionSettings)
    {
        connectionSettings.DefaultMappingFor<WorkflowExecutionLogRecord>(m => 
            m.IndexName(IElasticConfiguration.ResolveIndexName<WorkflowExecutionLogRecord>(IndexName)));
    }
}