using Elsa.IO.Common;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling byte array content.
/// </summary>
public class ByteArrayContentStrategy : IContentResolverStrategy
{
    public float Priority { get; init; } = Constants.StrategyPriorities.ByteArray;

    /// <inheritdoc />
    public bool CanResolve(object content) => content is byte[];

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var bytes = (byte[])content;
        var stream = new MemoryStream(bytes);
        return Task.FromResult<Stream>(stream);
    }
}