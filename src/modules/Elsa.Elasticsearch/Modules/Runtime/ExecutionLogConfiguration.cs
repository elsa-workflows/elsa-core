using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Elasticsearch.Modules.Runtime;

public class ExecutionLogConfiguration : ElasticConfiguration<WorkflowExecutionLogRecord>
{
    public override void Apply(ElasticsearchClientSettings settings, IDictionary<Type, string> indexConfig)
    {
        settings.DefaultMappingFor<WorkflowExecutionLogRecord>(m => 
            m.IndexName(indexConfig[typeof(WorkflowExecutionLogRecord)]));
    }
}