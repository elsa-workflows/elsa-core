using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Common;

public abstract class ElasticConfiguration<T> : IElasticConfiguration
{
    public abstract void Apply(ElasticsearchClientSettings settings, IDictionary<Type, string> indexConfig);
}