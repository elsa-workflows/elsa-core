namespace Elsa.Elasticsearch.Services;

public interface IIndexRolloverStrategy
{
    Task ApplyAsync(CancellationToken cancellationToken = default);
}