using System.Text;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling plain text content by encoding as UTF-8.
/// </summary>
public class TextContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is string;

    /// <inheritdoc />
    public Task<(Stream Stream, string Name)> ResolveAsync(object content, string? name = null, CancellationToken cancellationToken = default)
    {
        var textContent = (string)content;
        var textBytes = Encoding.UTF8.GetBytes(textContent);
        var stream = new MemoryStream(textBytes);
        var resolvedName = name ?? "file.txt";
        return Task.FromResult((stream, resolvedName));
    }
}