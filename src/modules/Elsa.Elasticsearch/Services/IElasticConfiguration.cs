using Nest;

namespace Elsa.Elasticsearch.Services;

public interface IElasticConfiguration
{
    void Apply(ConnectionSettings connectionSettings);
}