using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Runtime.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Runtime;

public class ExecutionLogConfiguration : ElasticConfiguration<WorkflowExecutionLogRecord>
{
    public override void Apply(ConnectionSettings connectionSettings, IDictionary<string, string> aliasConfig)
    {
        connectionSettings.DefaultMappingFor<WorkflowExecutionLogRecord>(m => 
            m.IndexName(aliasConfig[nameof(WorkflowExecutionLogRecord)]));
    }
}