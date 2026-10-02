using Elsa.IO.Common;
using Elsa.IO.Extensions;
using Elsa.IO.Models;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling file path content by reading from the filesystem.
/// </summary>
public class FilePathContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public float Priority => Constants.StrategyPriorities.FilePath;

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
    public Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = (string)content;
            filePath = ResolveActualPath(filePath);
            
            var fileName = Path.GetFileName(filePath);
            var fileStream = File.OpenRead(filePath);
            
            var result = new BinaryContent
            {
                Name = fileName.GetNameAndExtension(),
                Stream = fileStream
            };
            
            return Task.FromResult(result);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new FileNotFoundException($"Error opening file: {content}", content.ToString(), ex);
        }
    }
    
    private string ResolveActualPath(string filePath)
    {
        filePath = filePath.CleanFilePath();

        if (Path.IsPathRooted(filePath) && File.Exists(filePath))
            return filePath;
            
        var normalized = Path.GetFullPath(filePath);
        if (File.Exists(normalized))
            return normalized;
            
        var combined = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), filePath));
        if (File.Exists(combined))
            return combined;

        throw new FileNotFoundException($"Could not find file at path: {filePath}", filePath);
    }
}