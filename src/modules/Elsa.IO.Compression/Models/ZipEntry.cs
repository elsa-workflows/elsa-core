using JetBrains.Annotations;

namespace Elsa.IO.Compression.Models;

/// <summary>
/// Represents a zip entry with content and metadata.
/// </summary>
/// <param name="Content">The content of the zip entry. Can be byte[], Stream, file path, file URL, or base64 string.</param>
/// <param name="EntryName">The name of the entry in the zip archive.</param>
[UsedImplicitly]
public record ZipEntry(object Content, string? EntryName = null);