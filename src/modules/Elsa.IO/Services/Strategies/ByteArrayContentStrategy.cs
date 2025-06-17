namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling byte array content.
/// </summary>
public class ByteArrayContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is byte[];

    /// <inheritdoc />
    public Task<(Stream Stream, string Name)> ResolveAsync(object content, string? name = null, CancellationToken cancellationToken = default)
    {
        var bytes = (byte[])content;
        var stream = new MemoryStream(bytes);
        var resolvedName = name ?? "file.bin";
        return Task.FromResult((stream, resolvedName));
    }
}