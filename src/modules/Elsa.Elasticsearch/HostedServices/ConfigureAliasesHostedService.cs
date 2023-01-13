using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elsa.Elasticsearch.Options;
using Elsa.Elasticsearch.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Elasticsearch.HostedServices;

/// <summary>
/// Configures aliases.
/// </summary>
public class ConfigureAliasesHostedService : IHostedService
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly IEnumerable<IIndexConfiguration> _configurations;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigureAliasesHostedService(ElasticsearchClient client, IOptions<ElasticsearchOptions> options, IEnumerable<IIndexConfiguration> configurations)
    {
        _client = client;
        _options = options.Value;
        _configurations = configurations;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConfigureClientAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task ConfigureClientAsync(CancellationToken cancellationToken)
    {
        foreach (var configuration in _configurations) 
            await configuration.ConfigureClientAsync(_client, cancellationToken);
        
        foreach (var configuration in _configurations)
        {
            var alias = _options.GetIndexNameFor(configuration.DocumentType);
            var indexName = configuration.IndexNamingStrategy.GenerateName(alias);
            var indexExists = (await _client.Indices.ExistsAsync(indexName, cancellationToken)).Exists;

            if (indexExists)
                continue;

            var response = await _client.Indices.CreateAsync(indexName, c => c
                .Aliases(a => a.Add(alias, new Alias { IsWriteIndex = true })), cancellationToken);

            if (response.IsValidResponse)
                continue;

            if (response.TryGetOriginalException(out var exception))
                throw exception!;
        }
    }
}