namespace Elsa.IO.Services;

/// <summary>
/// Provides methods to resolve various content types to streams.
/// </summary>
public interface IContentResolver
{
    /// <summary>
    /// Resolves arbitrary content to a stream with a resolved name.
    /// </summary>
    /// <param name="content">The content to resolve. Can be byte[], Stream, file path, file URL, base64 string, or plain text.</param>
    /// <param name="name">Optional name hint for the content.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A stream containing the content and the resolved name.</returns>
    Task<(Stream Stream, string Name)> ResolveContentAsync(object content, string? name = null, CancellationToken cancellationToken = default);
}