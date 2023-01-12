using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Common;

namespace Elsa.Elasticsearch.Services;

public interface IElasticConfiguration
{
    void Apply(ElasticsearchClientSettings settings, IDictionary<Type,string> indexConfig);

    public static IDictionary<Type, string> GetDefaultIndexConfig()
    {
        return Utils.GetElasticDocumentTypes().ToDictionary(type => type, type => type.Name.ToLower());
    }
}