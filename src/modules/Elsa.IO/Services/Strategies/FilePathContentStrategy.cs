namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling file path content by reading from the filesystem.
/// </summary>
public class FilePathContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is string str && File.Exists(str);

    /// <inheritdoc />
    public Task<(Stream Stream, string Name)> ResolveAsync(object content, string? name = null, CancellationToken cancellationToken = default)
    {
        var filePath = (string)content;
        var fileName = name ?? Path.GetFileName(filePath);
        var fileStream = File.OpenRead(filePath);
        return Task.FromResult((fileStream, fileName));
    }
}