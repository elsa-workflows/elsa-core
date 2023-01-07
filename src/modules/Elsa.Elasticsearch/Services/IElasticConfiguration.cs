using Elsa.Elasticsearch.Common;
using Nest;

namespace Elsa.Elasticsearch.Services;

public interface IElasticConfiguration
{
    void Apply(ConnectionSettings connectionSettings, IDictionary<string,string> aliasConfig);

    internal static IDictionary<string,string> GetDefaultAliasConfig()
    {
        var types = Utils.GetElasticDocumentTypes();
        return new Dictionary<string, string>(types.Select(t => new KeyValuePair<string, string>(t.Name, t.Name)));
    }
}