namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling byte array content.
/// </summary>
public class ByteArrayContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is byte[];

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var bytes = (byte[])content;
        var stream = new MemoryStream(bytes);
        return Task.FromResult<Stream>(stream);
    }
}