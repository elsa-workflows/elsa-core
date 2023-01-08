using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Workflows.Runtime.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Runtime;

public class ExecutionLogConfiguration : IElasticConfiguration
{
    public Type DocumentType() => typeof(WorkflowExecutionLogRecord);
    
    public void Apply(ConnectionSettings connectionSettings, IDictionary<string,string> aliasConfig)
    {
        connectionSettings.DefaultMappingFor<WorkflowExecutionLogRecord>(m => 
            m.IndexName(aliasConfig[nameof(WorkflowExecutionLogRecord)]));
    }
}