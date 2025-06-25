namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Defines a strategy for resolving specific content types to streams.
/// </summary>
public interface IContentResolverStrategy
{
    internal float Priority { get; init; }
    
    /// <summary>
    /// Determines if this strategy can handle the specified content.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if this strategy can handle the content, false otherwise.</returns>
    bool CanResolve(object content);

    /// <summary>
    /// Resolves the content to a stream.
    /// </summary>
    /// <param name="content">The content to resolve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A stream containing the content.</returns>
    Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default);
}