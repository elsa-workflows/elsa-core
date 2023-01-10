using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.Hosting;
using Nest;

namespace Elsa.Elasticsearch.HostedServices;

public class ConfigureMappingHostedService : IHostedService
{
    private readonly ElasticClient _elasticClient;

    public ConfigureMappingHostedService(ElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _elasticClient.Indices.PutMappingAsync<WorkflowInstance>(
            descriptor => descriptor
                .Properties(p => p
                    .Flattened(d => d
                        .Name(p => p.WorkflowState.Properties))),
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}