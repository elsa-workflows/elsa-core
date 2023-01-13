using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Options;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Options;

namespace Elsa.Elasticsearch.Modules.Runtime;

/// <summary>
/// Configures Elasticsearch with mappings for <see cref="WorkflowExecutionLogRecord"/>.
/// </summary>
public class ExecutionLogConfiguration : IndexConfiguration<WorkflowExecutionLogRecord>
{
    private readonly ElasticsearchOptions _options;

    /// <inheritdoc />
    public ExecutionLogConfiguration(IOptions<ElasticsearchOptions> options) => _options = options.Value;

    /// <inheritdoc />
    public override void ConfigureClientSettings(ElasticsearchClientSettings settings) => 
        settings.DefaultMappingFor<WorkflowExecutionLogRecord>(m => m
            .IndexName(_options.GetIndexNameFor<WorkflowExecutionLogRecord>()));
}