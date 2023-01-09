using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Services;
using Nest;

namespace Elsa.Elasticsearch.Implementations.RolloverStrategies;

public class RolloverOnMonthlyBasis : IRolloverStrategy
{
    private readonly ElasticClient _client;
    
    public RolloverOnMonthlyBasis(ElasticClient client)
    {
        _client = client;
    }

    public void Apply(IEnumerable<Type> types, IDictionary<Type, string> aliasConfig)
    {
        foreach (var type in types)
        {
            var aliasName = aliasConfig[type];
            var indexName = Utils.GenerateIndexName(aliasName);
            
            var indexExists = _client.Indices.Exists(indexName).Exists;

            if (indexExists) continue;
            
            var response = _client.Indices.Create(indexName, s => s
                .Aliases(a => a.Alias(aliasName))
                .Map(m => m.AutoMap(type)));
                
            if (response.IsValid) continue;
            throw response.OriginalException;
        }
    }
}