namespace Elsa.IO.Extensions;

public static class FilePathExtensions
{
    public static string CleanFilePath(this string filePath)
    {
        // Clean up the path - trim quotes and whitespace that might come from copy-paste
        filePath = filePath.Trim().Trim('"', '\'');

        // Replace backslashes with forward slashes on Unix/Mac systems
        if (Path.DirectorySeparatorChar == '/')
        {
            filePath = filePath.Replace('\\', '/');
        }

        return filePath;
    }
    
    /// <summary>
    /// Gets the filename from the Content-Disposition header.
    /// </summary>
    public static string? GetFilename(this HttpResponseMessage response)
    {
        var dictionary = response.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        return dictionary.GetFilename();
    }
    
    /// <summary>
    /// Gets the filename from the Content-Disposition header.
    /// </summary>
    public static string? GetFilename(this IDictionary<string, string[]> headers)
    {
        if (!headers.TryGetValue("Content-Disposition", out var values)) 
            return null;
        
        var contentDispositionString = string.Join("", values);
        var contentDisposition = new System.Net.Mime.ContentDisposition(contentDispositionString);
        return contentDisposition.FileName;
    }
}