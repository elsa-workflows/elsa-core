using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Elsa.Workflows.Runtime.Entities;
using Nest;

namespace Elsa.Elasticsearch.Modules.Runtime;

public class ExecutionLogConfiguration : IElasticConfiguration
{
    public void Apply(ConnectionSettings connectionSettings)
    {
        connectionSettings.DefaultMappingFor<WorkflowExecutionLogRecord>(m => 
            m.IndexName(ElasticsearchOptions.Indices[typeof(WorkflowExecutionLogRecord)]));
    }
}