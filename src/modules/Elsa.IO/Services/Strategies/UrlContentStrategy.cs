using Elsa.IO.Common;
using Microsoft.Extensions.Logging;

namespace Elsa.IO.Services.Strategies;

/// <summary>
/// Strategy for handling URL content by downloading from HTTP/HTTPS URLs.
/// </summary>
public class UrlContentStrategy : IContentResolverStrategy
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<UrlContentStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlContentStrategy"/> class.
    /// </summary>
    public UrlContentStrategy(ILogger<UrlContentStrategy> logger, IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public float Priority { get; init; } = Constants.StrategyPriorities.Uri;

    /// <inheritdoc />
    public bool CanResolve(object content) => content is string str && (str.StartsWith("http://") || str.StartsWith("https://"));

    /// <inheritdoc />
    public async Task<Stream> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var url = (string)content;

        if (_httpClientFactory == null)
        {
            throw new InvalidOperationException("HTTP client factory is not available. Cannot download from URL.");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from URL: {Url}", url);
            throw;
        }
    }
}