using Elsa.Elasticsearch.Common;
using Nest;

namespace Elsa.Elasticsearch.Services;

public interface IElasticConfiguration
{
    void Apply(ConnectionSettings connectionSettings, IDictionary<Type,string> indexConfig);

    public static IDictionary<Type, string> GetDefaultIndexConfig()
    {
        return Utils.GetElasticDocumentTypes().ToDictionary(type => type, type => type.Name.ToLower());
    }
}