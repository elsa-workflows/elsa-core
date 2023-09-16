namespace Elsa.Http.Models;

/// <summary>
/// Represents a cached file.
/// </summary>
public class CachedFile
{
    /// <summary>
    /// An identifier for the file.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// The file name.
    /// </summary>
    public string Filename { get; set; } = default!;
    
    /// <summary>
    /// The file content type.
    /// </summary>
    public string ContentType { get; set; } = default!;
    
    /// <summary>
    /// The file content stream.
    /// </summary>
    public Stream Stream { get; set; } = default!;
}