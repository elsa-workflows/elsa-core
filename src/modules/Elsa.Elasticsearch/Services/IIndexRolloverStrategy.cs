namespace Elsa.Elasticsearch.Services;

/// <summary>
/// A rollover strategy that controls when and how indices are rolled over.
/// </summary>
public interface IIndexRolloverStrategy
{
    /// <summary>
    /// Applies the rollover.
    /// </summary>
    Task ApplyAsync(CancellationToken cancellationToken = default);
}