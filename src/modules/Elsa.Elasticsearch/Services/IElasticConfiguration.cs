using Elsa.Elasticsearch.Options;
using Nest;

namespace Elsa.Elasticsearch.Services;

public interface IElasticConfiguration
{
    void Apply(ConnectionSettings connectionSettings, IDictionary<string,string> indexConfig);
    
    public static string ResolveIndexName<T>(IDictionary<string,string> indices, string? indexName = default)
    {
        var indexNameFromConfig = indices[typeof(T).Name];
        return string.IsNullOrWhiteSpace(indexNameFromConfig) ? indexName : indexNameFromConfig;
    }
}