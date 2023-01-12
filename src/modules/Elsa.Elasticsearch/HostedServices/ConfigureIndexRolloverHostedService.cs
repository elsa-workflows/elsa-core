using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Elasticsearch.HostedServices;

/// <summary>
/// A recurring job that applies the rollover strategy every interval.
/// </summary>
public class ConfigureIndexRolloverHostedService : BackgroundService
{
    private readonly ElasticsearchClient _client;
    private readonly IEnumerable<IElasticConfiguration> _configurations;
    private readonly ElasticsearchOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigureIndexRolloverHostedService(ElasticsearchClient client, IEnumerable<IElasticConfiguration> configurations, IOptions<ElasticsearchOptions> options)
    {
        _client = client;
        _configurations = configurations;
        _options = options.Value;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_options.RolloverInterval, stoppingToken);
            await RolloverAsync(stoppingToken);
        }
    }

    private async Task RolloverAsync(CancellationToken cancellationToken)
    {
        foreach (var configuration in _configurations)
        {
            var alias = _options.GetIndexNameFor(configuration.DocumentType);
            var newIndexName = configuration.IndexNamingStrategy.GenerateName(alias);
            var indexExists = (await _client.Indices.ExistsAsync(newIndexName, cancellationToken)).Exists;

            if (indexExists)
                continue;

            await _client.Indices.RolloverAsync(alias, cfg => cfg.NewIndex(newIndexName), cancellationToken);
        }
    }
}