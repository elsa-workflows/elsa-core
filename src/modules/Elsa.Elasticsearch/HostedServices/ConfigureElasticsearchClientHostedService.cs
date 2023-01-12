using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.Elasticsearch.HostedServices;

/// <summary>
/// Configures the Elasticsearch client.
/// </summary>
public class ConfigureElasticsearchClientHostedService : IHostedService
{
    private readonly ElasticsearchClient _elasticClient;
    private readonly IEnumerable<IElasticConfiguration> _configurations;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigureElasticsearchClientHostedService(ElasticsearchClient elasticClient, IEnumerable<IElasticConfiguration> configurations)
    {
        _elasticClient = elasticClient;
        _configurations = configurations;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var configuration in _configurations) 
            await configuration.ConfigureClient(_elasticClient, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}