using System.Text.Json.Serialization;

namespace Elsa.Http.Models;

/// <summary>
/// Represents a downloadable object.
/// </summary>
public class HttpFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Downloadable"/> class.
    /// </summary>
    [JsonConstructor]
    public HttpFile()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpFile"/> class.
    /// </summary>
    /// <param name="stream">The stream to download.</param>
    /// <param name="filename">The filename to use when downloading the stream.</param>
    /// <param name="contentType">The content type to use when downloading the stream.</param>
    /// <param name="eTag">The ETag to use when downloading the stream.</param>
    public HttpFile(Stream stream, string? filename = default, string? contentType = default, string? eTag = default)
    {
        Stream = stream;
        Filename = filename;
        ContentType = contentType;
        ETag = eTag;
    }

    /// <summary>
    /// The file stream.
    /// </summary>
    public Stream Stream { get; set; } = default!;

    /// <summary>
    /// The filename.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// The content type.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The ETag.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Gets the file bytes.
    /// </summary>
    public byte[] GetBytes()
    {
        using var memoryStream = new MemoryStream();
        if (Stream.CanSeek) Stream.Seek(0, SeekOrigin.Begin);
        Stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}