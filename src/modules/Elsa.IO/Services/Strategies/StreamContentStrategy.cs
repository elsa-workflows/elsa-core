namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling Stream content.
/// </summary>
public class StreamContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is Stream;

    /// <inheritdoc />
    public Task<(Stream Stream, string Name)> ResolveAsync(object content, string? name = null, CancellationToken cancellationToken = default)
    {
        var stream = (Stream)content;
        var resolvedName = name ?? "file.bin";
        return Task.FromResult((stream, resolvedName));
    }
}