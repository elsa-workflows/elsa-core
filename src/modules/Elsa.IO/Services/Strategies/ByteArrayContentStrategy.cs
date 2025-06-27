using Elsa.IO.Common;
using Elsa.IO.Models;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling byte array content.
/// </summary>
public class ByteArrayContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public float Priority => Constants.StrategyPriorities.ByteArray;

    /// <inheritdoc />
    public bool CanResolve(object content) => content is byte[];

    /// <inheritdoc />
    public Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var bytes = (byte[])content;
        var stream = new MemoryStream(bytes);
        
        var result = new BinaryContent
        {
            Stream = stream,
            Name = "data", // Generic name since we don't have specific information
            Extension = string.Empty
        };
        
        return Task.FromResult(result);
    }
}