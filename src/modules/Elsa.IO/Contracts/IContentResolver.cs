namespace Elsa.IO.Contracts;

/// <summary>
/// Provides methods to resolve various content types to streams.
/// </summary>
public interface IContentResolver
{
    /// <summary>
    /// Resolves arbitrary content to a stream.
    /// </summary>
    /// <param name="content">The content to resolve. Can be byte[], Stream, file path, file URL, base64 string, or plain text.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A stream containing the content.</returns>
    Task<Stream> ResolveContentAsync(object content, CancellationToken cancellationToken = default);
}