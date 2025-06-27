namespace Elsa.IO.Contracts;

using Elsa.IO.Models;

/// <summary>
/// Provides methods to resolve various content types to BinaryContent.
/// </summary>
public interface IContentResolver
{
    /// <summary>
    /// Resolves arbitrary content to a BinaryContent object that includes the content stream and metadata.
    /// </summary>
    /// <param name="content">The content to resolve. Can be byte[], Stream, file path, file URL, base64 string, or plain text.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A BinaryContent object containing the content stream and associated metadata.</returns>
    Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default);
}