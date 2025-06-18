namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling file path content by reading from the filesystem.
/// </summary>
public class FilePathContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content) => content is string str && File.Exists(str);

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var filePath = (string)content;
        var fileStream = File.OpenRead(filePath);
        return Task.FromResult<Stream>(fileStream);
    }
}