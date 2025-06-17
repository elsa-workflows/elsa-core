namespace Elsa.IO.Services;

/// <summary>
/// Defines a strategy for resolving specific content types to streams.
/// </summary>
public interface IContentResolverStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the specified content.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if this strategy can handle the content, false otherwise.</returns>
    bool CanHandle(object content);

    /// <summary>
    /// Resolves the content to a stream with a resolved name.
    /// </summary>
    /// <param name="content">The content to resolve.</param>
    /// <param name="name">Optional name hint for the content.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A stream containing the content and the resolved name.</returns>
    Task<(Stream Stream, string Name)> ResolveAsync(object content, string? name = null, CancellationToken cancellationToken = default);
}