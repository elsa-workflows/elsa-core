using Elsa.IO.Common;
using Elsa.IO.Extensions;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling file path content by reading from the filesystem.
/// </summary>
public class FilePathContentStrategy : IContentResolverStrategy
{
    public float Priority { get; init; } = Constants.StrategyPriorities.FilePath;

    /// <inheritdoc />
    public bool CanResolve(object content)
    {
        if (content is not string filePath)
        {
            return false;
        }

        filePath = filePath.CleanFilePath();

        try
        {
            if (Path.IsPathRooted(filePath) && File.Exists(filePath))
            {
                return true;
            }
            
            var normalized = Path.GetFullPath(filePath);
            if (File.Exists(normalized))
            {
                return true;
            }
            
            var combined = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), filePath));
            return File.Exists(combined);
        }
        catch (Exception)
        {
            return false;
        }
    } 

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var filePath = (string)content;
        filePath = filePath.CleanFilePath();

        try
        {
            if (Path.IsPathRooted(filePath) && File.Exists(filePath))
            {
                var fileStream = File.OpenRead(filePath);
                return Task.FromResult<Stream>(fileStream);
            }

            var normalized = Path.GetFullPath(filePath);
            if (File.Exists(normalized))
            {
                var fileStream = File.OpenRead(normalized);
                return Task.FromResult<Stream>(fileStream);
            }
            
            var combined = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), filePath));

            if (File.Exists(combined))
            {
                var fileStream = File.OpenRead(combined);
                return Task.FromResult<Stream>(fileStream);
            }

            throw new FileNotFoundException($"Could not find file at path: {filePath}", filePath);
        }
        catch (Exception ex) when (!(ex is FileNotFoundException))
        {
            throw new FileNotFoundException($"Error opening file: {filePath}", filePath, ex);
        }
    }
}