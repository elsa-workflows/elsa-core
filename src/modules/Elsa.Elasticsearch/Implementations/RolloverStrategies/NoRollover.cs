using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Implementations.RolloverStrategies;

/// <summary>
/// No rollover.
/// </summary>
public class NoRollover : IIndexRolloverStrategy
{
    /// <inheritdoc />
    public Task ApplyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}