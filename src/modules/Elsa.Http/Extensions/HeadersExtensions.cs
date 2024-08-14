// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for HTTP headers.
/// </summary>
public static class HeadersExtensions
{
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