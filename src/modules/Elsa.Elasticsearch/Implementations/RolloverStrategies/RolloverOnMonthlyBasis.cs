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
        
        //foreach (var (currentIndexName, aliasPointingCurrentIndex) in await GetAliasesWithTargetIndices(cancellationToken))
        foreach (var alias in aliases)
        {
            var newIndexName = _indexNamingStrategy.GenerateName(alias);
                
            var indexExists = (await _client.Indices.ExistsAsync(newIndexName, cancellationToken)).Exists;
            if (indexExists) continue;
            
            //await _client.Indices.CreateAsync(newIndexName, cancellationToken);

            await _client.Indices.RolloverAsync(
                alias, 
                cfg => cfg.NewIndex(newIndexName),
                cancellationToken);
                
            // // Update alias to recognize current index as read-only
            // await _client.Indices.DeleteAliasAsync(currentIndexName, aliasPointingCurrentIndex, cancellationToken);
            // await _client.Indices.PutAliasAsync(currentIndexName, aliasPointingCurrentIndex, 
            //     cfg => cfg.IsWriteIndex(false), cancellationToken);
            //
            // // Point the alias to the new index
            // await _client.Indices.PutAliasAsync(newIndexName, aliasPointingCurrentIndex,
            //     cfg => cfg.IsWriteIndex(), cancellationToken);
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> GetAliasesWithTargetIndices(CancellationToken cancellationToken)
    {
        var aliasesWithIndices = new Dictionary<string, string>();
        var indicesResponse = await _client.Indices.GetAsync(Indices.All, cancellationToken);
        
        var groupOfIndexWithAliases = indicesResponse.Indices.Values.Select(s => s
            .Aliases!.Where(a => a.Value.IsWriteIndex != null && (bool)a.Value.IsWriteIndex));
        
        foreach (var indexWithAliasList in groupOfIndexWithAliases)
        {
            // Only 1 alias exists per index in Elsa Elasticsearch configuration
            var alias = indexWithAliasList.Single();
            
            aliasesWithIndices.TryAdd(alias.Key.ToString(), alias.Value.ToString()!);
        }
        return aliasesWithIndices;
    }
}