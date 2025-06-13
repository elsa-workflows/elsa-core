using System.Text.Json.Serialization;

namespace Elsa.Compression.Models;

/// <summary>
/// Represents a zip entry with content and metadata.
/// </summary>
public class ZipEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ZipEntry"/> class.
    /// </summary>
    [JsonConstructor]
    public ZipEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipEntry"/> class.
    /// </summary>
    /// <param name="content">The content of the entry.</param>
    /// <param name="entryName">The name of the entry in the archive.</param>
    public ZipEntry(object content, string entryName)
    {
        Content = content;
        EntryName = entryName;
    }

    /// <summary>
    /// The content of the zip entry. Can be byte[], Stream, file path, file URL, or base64 string.
    /// </summary>
    public object Content { get; set; } = default!;

    /// <summary>
    /// The name of the entry in the zip archive.
    /// </summary>
    public string EntryName { get; set; } = default!;
}