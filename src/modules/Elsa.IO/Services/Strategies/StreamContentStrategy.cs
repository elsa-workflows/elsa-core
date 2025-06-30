using Elsa.IO.Common;
using Elsa.IO.Extensions;
using Elsa.IO.Models;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling Stream content.
/// </summary>
public class StreamContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public float Priority => Constants.StrategyPriorities.Stream;

    /// <inheritdoc />
    public bool CanResolve(object content) => content is Stream;

    /// <inheritdoc />
    public Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var stream = (Stream)content;
        
        string? name = null;
        if (stream is FileStream fileStream)
        {
            name = Path.GetFileName(fileStream.Name);
        }
        
        var result = new BinaryContent
        {
            Stream = stream,
            Name = name?.GetNameAndExtension() ?? "data.bin",
        };
        
        return Task.FromResult(result);
    }
}