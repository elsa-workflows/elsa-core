using Elsa.Http.Contexts;
using Elsa.Http.Models;

namespace Elsa.Http.Contracts;

/// <summary>
/// Provides downloadables from the specified content, if supported.
/// </summary>
public interface IDownloadableContentHandler
{
    /// <summary>
    /// The priority of this provider. Providers with lower priority are tried first.
    /// </summary>
    float Priority { get; }
    
    /// <summary>
    /// Returns true if this provider supports the specified content.
    /// </summary>
    /// <param name="content">The content to check.</param>
    bool GetSupportsContent(object content);
    
    /// <summary>
    /// Returns a list of downloadables from the specified content.
    /// </summary>
    IEnumerable<Func<ValueTask<Downloadable>>> GetDownloadablesAsync(DownloadableContext context);
}