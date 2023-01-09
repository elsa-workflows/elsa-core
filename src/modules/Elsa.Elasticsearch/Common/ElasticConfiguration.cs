using Elsa.Elasticsearch.Services;
using Nest;

namespace Elsa.Elasticsearch.Common;

public abstract class ElasticConfiguration<T> : IElasticConfiguration
{
    public abstract void Apply(ConnectionSettings connectionSettings, IDictionary<Type, string> indexConfig);
}