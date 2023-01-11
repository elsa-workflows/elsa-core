using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Implementations.RolloverStrategies;

public class RolloverOnMonthlyBasis : IIndexRolloverStrategy
{
    private readonly ElasticsearchClient _client;
    private readonly IIndexNamingStrategy _indexNamingStrategy;
    
    public RolloverOnMonthlyBasis(ElasticsearchClient client, IIndexNamingStrategy indexNamingStrategy)
    {
        _client = client;
        _indexNamingStrategy = indexNamingStrategy;
    }

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        var getAliasResponse = await _client.Indices.GetAliasAsync(cancellationToken);
        var aliases = getAliasResponse.Aliases.Values.SelectMany(s => s.Aliases.Select(a => a.Key));
        
        foreach (var alias in aliases)
        {
            var newIndexName = _indexNamingStrategy.GenerateName(alias);
                
            var indexExists = (await _client.Indices.ExistsAsync(newIndexName, cancellationToken)).Exists;
            if (indexExists) continue;

            await _client.Indices.RolloverAsync(
                alias, 
                cfg => cfg.NewIndex(newIndexName),
                cancellationToken);
        }
    }
}