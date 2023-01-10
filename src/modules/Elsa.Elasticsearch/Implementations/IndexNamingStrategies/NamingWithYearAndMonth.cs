using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Services;
using Nest;

namespace Elsa.Elasticsearch.Implementations.IndexNamingStrategies;

public class NamingWithYearAndMonth : IIndexNamingStrategy
{
    private readonly ElasticClient _client;
    
    public NamingWithYearAndMonth(ElasticClient client)
    {
        _client = client;
    }

    public void Apply(IEnumerable<Type> typesToConfigure, IDictionary<Type, string> aliasConfig)
    {
        foreach (var type in typesToConfigure)
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