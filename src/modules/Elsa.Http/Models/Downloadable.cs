using System.Text.Json.Serialization;

namespace Elsa.Http.Models;

/// <summary>
/// Represents a downloadable object.
/// </summary>
public class Downloadable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Downloadable"/> class.
    /// </summary>
    [JsonConstructor]
    public Downloadable()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Downloadable"/> class.
    /// </summary>
    /// <param name="stream">The stream to download.</param>
    /// <param name="filename">The filename to use when downloading the stream.</param>
    /// <param name="contentType">The content type to use when downloading the stream.</param>
    /// <param name="eTag">The ETag to use when downloading the stream.</param>
    public Downloadable(Stream stream, string? filename = default, string? contentType = default, string? eTag = default)
    {
        Stream = stream;
        Filename = filename;
        ContentType = contentType;
        ETag = eTag;
    }

    /// <summary>
    /// The stream to download.
    /// </summary>
    public Stream Stream { get; set; } = default!;
    
    /// <summary>
    /// The filename to use when downloading the stream.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// The content type to use when downloading the stream.
    /// </summary>
    public string? ContentType { get; set; }
    
    /// <summary>
    /// The ETag to use when downloading the stream.
    /// </summary>
    public string? ETag { get; set; }
}