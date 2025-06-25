using System.Text;
using Elsa.IO.Common;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling plain text content by encoding as UTF-8.
/// </summary>
public class TextContentStrategy : IContentResolverStrategy
{
    public float Priority { get; init; } = Constants.StrategyPriorities.Text;

    /// <inheritdoc />
    public bool CanResolve(object content) => content is string;

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var textContent = (string)content;
        var textBytes = Encoding.UTF8.GetBytes(textContent);
        var stream = new MemoryStream(textBytes);
        return Task.FromResult<Stream>(stream);
    }
}