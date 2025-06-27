namespace Elsa.IO.Services.Strategies;

using Elsa.IO.Models;

/// <summary>
/// Defines a strategy for resolving specific content types to BinaryContent.
/// </summary>
public interface IContentResolverStrategy
{
    /// <summary>
    /// The priority of the strategy.
    /// </summary>
    float Priority { get; }
    
    /// <summary>
    /// Determines if this strategy can handle the specified content.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if this strategy can handle the content, false otherwise.</returns>
    bool CanResolve(object content);

    /// <summary>
    /// Resolves the content to a BinaryContent object that includes the content stream and metadata.
    /// </summary>
    /// <param name="content">The content to resolve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A BinaryContent object containing the content stream and associated metadata.</returns>
    Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default);
}