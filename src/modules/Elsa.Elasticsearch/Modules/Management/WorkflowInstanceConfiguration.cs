using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Options;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.Options;

namespace Elsa.Elasticsearch.Modules.Management;

/// <summary>
/// Configures Elasticsearch with mappings for <see cref="WorkflowInstance"/>.
/// </summary>
public class WorkflowInstanceConfiguration : IndexConfiguration<WorkflowInstance>
{
    private readonly ElasticsearchOptions _options;

    /// <inheritdoc />
    public WorkflowInstanceConfiguration(IOptions<ElasticsearchOptions> options)
    {
        _options = options.Value;
    }
    
    /// <inheritdoc />
    public override void ConfigureClientSettings(ElasticsearchClientSettings settings)
    {
        var alias = _options.GetIndexNameFor<WorkflowInstance>();
        var indexName = IndexNamingStrategy.GenerateName(alias);
        settings.DefaultMappingFor<WorkflowInstance>(m => m.IndexName(indexName));
    }

    /// <inheritdoc />
    public override async ValueTask ConfigureClientAsync(ElasticsearchClient client, CancellationToken cancellationToken)
    {
        await client.Indices.CreateAsync<WorkflowInstance>(
            descriptor => descriptor.Mappings(m => m
                .Properties(p => p
                    .Flattened(d => d.WorkflowState.Properties))),
            cancellationToken);
    }
}