namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling Stream content.
/// </summary>
public class StreamContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is Stream;

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var stream = (Stream)content;
        return Task.FromResult(stream);
    }
}