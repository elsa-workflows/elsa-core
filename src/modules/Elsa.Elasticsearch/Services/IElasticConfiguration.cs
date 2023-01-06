using Elsa.Elasticsearch.Options;
using Nest;

namespace Elsa.Elasticsearch.Services;

public interface IElasticConfiguration
{
    void Apply(ConnectionSettings connectionSettings, ElasticsearchOptions options);
    
    public static string ResolveIndexName<T>(ElasticsearchOptions options, string indexName)
    {
        var indexNameFromConfig = options.Indices[typeof(T).Name];
        return string.IsNullOrWhiteSpace(indexNameFromConfig) ? indexName : indexNameFromConfig;
    }
}