using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Runtime.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Runtime;

public class ExecutionLogConfiguration : ElasticConfiguration<WorkflowExecutionLogRecord>
{
    public override void Apply(ConnectionSettings connectionSettings, IDictionary<Type, string> indexConfig)
    {
        connectionSettings.DefaultMappingFor<WorkflowExecutionLogRecord>(m => 
            m.IndexName(indexConfig[typeof(WorkflowExecutionLogRecord)]));
    }
}