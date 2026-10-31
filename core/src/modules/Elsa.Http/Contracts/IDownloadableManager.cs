using Elsa.Http.Options;

namespace Elsa.Http;

/// <summary>
/// Provides downloadables from the specified content, if supported.
/// </summary>
public interface IDownloadableManager
{
    /// <summary>
    /// Returns a list of downloadables from the specified content.
    /// </summary>
    IEnumerable<Func<ValueTask<Downloadable>>> GetDownloadablesAsync(object content, DownloadableOptions? options = default, CancellationToken cancellationToken = default);
}