using Elsa.Elasticsearch.Common;
using Nest;

namespace Elsa.Elasticsearch.Services;

public interface IElasticConfiguration
{
    void Apply(ConnectionSettings connectionSettings, IDictionary<string,string> aliasConfig);

    public static IDictionary<string, string> GetDefaultAliasConfig()
    {
        return Utils.GetElasticDocumentTypes().ToDictionary(type => type.Name, type => type.Name.ToLower());
    }
}