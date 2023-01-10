namespace Elsa.Elasticsearch.Services;

public interface IIndexRolloverStrategy
{
    Task ApplyAsync(IEnumerable<Type> types, CancellationToken cancellationToken = default);
}