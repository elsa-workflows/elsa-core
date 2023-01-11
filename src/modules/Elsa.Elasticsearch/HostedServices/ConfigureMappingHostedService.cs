using Elastic.Clients.Elasticsearch;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.Hosting;

namespace Elsa.Elasticsearch.HostedServices;

public class ConfigureMappingHostedService : IHostedService
{
    private readonly ElasticsearchClient _elasticClient;

    public ConfigureMappingHostedService(ElasticsearchClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _elasticClient.Indices.CreateAsync<WorkflowInstance>(
            descriptor => descriptor.Mappings(m => m
                .Properties(p => p
                    .Flattened(d => d.WorkflowState.Properties))),
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}