using System.Text;
using Elsa.IO.Common;
using Elsa.IO.Models;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling plain text content by encoding as UTF-8.
/// </summary>
public class TextContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public float Priority => Constants.StrategyPriorities.Text;

    /// <inheritdoc />
    public bool CanResolve(object content) => content is string;

    /// <inheritdoc />
    public Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var textContent = (string)content;
        var textBytes = Encoding.UTF8.GetBytes(textContent);
        var stream = new MemoryStream(textBytes);
        
        return Task.FromResult(new BinaryContent
        {
            Name = "text.txt",
            Stream = stream
        });
    }
}