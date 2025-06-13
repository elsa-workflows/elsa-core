using Elsa.Compression.Models;

namespace Elsa.Compression.Services;

/// <summary>
/// Provides methods to resolve zip entry content to streams.
/// </summary>
public interface IZipEntryContentResolver
{
    /// <summary>
    /// Resolves the content of a zip entry to a stream.
    /// </summary>
    /// <param name="entry">The zip entry containing the content.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A stream containing the entry content.</returns>
    Task<Stream> ResolveContentAsync(ZipEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves arbitrary content to a stream with a given entry name.
    /// </summary>
    /// <param name="content">The content to resolve.</param>
    /// <param name="entryName">The name of the entry.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A stream containing the content and the resolved entry name.</returns>
    Task<(Stream Stream, string EntryName)> ResolveContentAsync(object content, string? entryName = null, CancellationToken cancellationToken = default);
}