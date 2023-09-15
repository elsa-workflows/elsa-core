using Elsa.Http.Contracts;

namespace Elsa.Http.Contexts;

/// <summary>
/// Provides context for downloadable providers.
/// </summary>
/// <param name="Manager">The manager.</param>
/// <param name="Content">The content to get downloadables from.</param>
/// <param name="CancellationToken">The cancellation token.</param>
public record DownloadableContext(IDownloadableManager Manager, object Content, CancellationToken CancellationToken);