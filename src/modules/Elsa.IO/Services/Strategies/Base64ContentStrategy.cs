namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling base64 encoded content.
/// </summary>
public class Base64ContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is string str && str.StartsWith("base64:");

    /// <inheritdoc />
    public Task<(Stream Stream, string Name)> ResolveAsync(object content, string? name = null, CancellationToken cancellationToken = default)
    {
        var str = (string)content;
        var base64Content = str.Substring(7); // Remove "base64:" prefix
        var base64Bytes = Convert.FromBase64String(base64Content);
        var stream = new MemoryStream(base64Bytes);
        var resolvedName = name ?? "file.bin";
        return Task.FromResult((stream, resolvedName));
    }
}