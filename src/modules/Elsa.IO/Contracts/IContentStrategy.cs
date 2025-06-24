namespace Elsa.IO.Contracts;

/// <summary>
/// Contract for content strategies to handle different types of content.
/// </summary>
public interface IContentStrategy
{
    /// <summary>
    /// Determines whether the strategy can handle the provided content.
    /// </summary>
    bool CanHandle(object content);
    
    /// <summary>
    /// Parses the provided content and returns a <see cref="Stream"/>.
    /// </summary>
    Task<Stream> ParseContentAsync(object content, CancellationToken cancellationToken);
}