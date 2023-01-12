using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Services;
using Elsa.Elasticsearch.Strategies;

namespace Elsa.Elasticsearch.Common;

/// <summary>
/// A convenience base class for document type configurations.
/// </summary>
public abstract class ElasticConfiguration<T> : IElasticConfiguration
{
    /// <inheritdoc />
    public Type DocumentType => typeof(T);

    /// <inheritdoc />
    public virtual IIndexNamingStrategy IndexNamingStrategy => new DefaultNaming();

    /// <inheritdoc />
    public abstract void ConfigureClientSettings(ElasticsearchClientSettings settings);

    /// <inheritdoc />
    public virtual ValueTask ConfigureClientAsync(ElasticsearchClient client, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}