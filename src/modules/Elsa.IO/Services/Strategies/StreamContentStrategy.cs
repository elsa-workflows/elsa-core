using Elsa.IO.Common;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling Stream content.
/// </summary>
public class StreamContentStrategy : IContentResolverStrategy
{
    public float Priority { get; init; } = Constants.StrategyPriorities.Stream;

    /// <inheritdoc />
    public bool CanResolve(object content) => content is Stream;

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var stream = (Stream)content;
        return Task.FromResult(stream);
    }
}