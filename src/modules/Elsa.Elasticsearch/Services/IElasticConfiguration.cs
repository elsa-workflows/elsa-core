using Elsa.Elasticsearch.Options;
using Nest;

namespace Elsa.Elasticsearch.Services;

public interface IElasticConfiguration
{
    void Apply(ConnectionSettings connectionSettings);
    
    public static string ResolveIndexName<T>(string indexName)
    {
        var indexNameFromConfig = ElasticsearchOptions.Indices[typeof(T)];
        return string.IsNullOrWhiteSpace(indexName) ? indexNameFromConfig : indexName;
    }
}