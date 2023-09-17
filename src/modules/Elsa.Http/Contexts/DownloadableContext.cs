using Elsa.Http.Contracts;
using Elsa.Http.Options;

namespace Elsa.Http.Contexts;

/// <summary>
/// Provides context for downloadable providers.
/// </summary>
/// <param name="Manager">The manager.</param>
/// <param name="Content">The content to get downloadables from.</param>
/// <param name="ETag">An optional ETag.</param>
/// <param name="CancellationToken">The cancellation token.</param>
public record DownloadableContext(
    IDownloadableManager Manager, 
    object Content, 
    DownloadableOptions Options,
    CancellationToken CancellationToken);