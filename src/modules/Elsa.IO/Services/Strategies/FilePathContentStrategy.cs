namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling file path content by reading from the filesystem.
/// </summary>
public class FilePathContentStrategy : IContentResolverStrategy
{
    /// <inheritdoc />
    public bool CanHandle(object content)
    {
        if (content is not string str)
        {
            return false;
        }

        // Clean up the path - trim quotes and whitespace that might come from copy-paste
        str = str.Trim().Trim('"', '\'');
        
        // Replace backslashes with forward slashes on Unix/Mac systems
        if (Path.DirectorySeparatorChar == '/')
        {
            str = str.Replace('\\', '/');
        }
        
        try
        {
            // First try as absolute path
            if (Path.IsPathRooted(str) && File.Exists(str))
            {
                return true;
            }
            
            // Then try normalized path
            var normalized = Path.GetFullPath(str);
            if (File.Exists(normalized))
            {
                return true;
            }
            
            // Finally try relative to current directory
            var combined = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), str));
            return File.Exists(combined);
        }
        catch (Exception)
        {
            // If any path-related exception occurs, this is not a valid file path
            return false;
        }
    } 

    /// <inheritdoc />
    public Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var filePath = (string)content;
        
        // Clean up the path - trim quotes and whitespace
        filePath = filePath.Trim().Trim('"', '\'');
        
        // Replace backslashes with forward slashes on Unix/Mac systems
        if (Path.DirectorySeparatorChar == '/')
        {
            filePath = filePath.Replace('\\', '/');
        }
        
        try
        {
            // Try direct access first if it's a rooted path
            if (Path.IsPathRooted(filePath) && File.Exists(filePath))
            {
                var fileStream = File.OpenRead(filePath);
                return Task.FromResult<Stream>(fileStream);
            }
            
            // Try normalized path
            var normalized = Path.GetFullPath(filePath);
            if (File.Exists(normalized))
            {
                var fileStream = File.OpenRead(normalized);
                return Task.FromResult<Stream>(fileStream);
            }
            
            // Try combining with current directory
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