using Elsa.IO.Extensions;
using Elsa.IO.Http.Common;
using Elsa.IO.Models;
using Elsa.IO.Services.Strategies;
using Microsoft.Extensions.Logging;

namespace Elsa.IO.Http.Services.Strategies;

/// <summary>
/// Strategy for handling URL content by downloading from HTTP/HTTPS URLs.
/// </summary>
public class UrlContentStrategy(ILogger<UrlContentStrategy> logger, IHttpClientFactory httpClientFactory) : IContentResolverStrategy
{
    /// <inheritdoc />
    public float Priority => Constants.StrategyPriorities.Uri;

    /// <inheritdoc />
    public bool CanResolve(object content) => content is string str && (str.StartsWith("http://") || str.StartsWith("https://"));

    /// <inheritdoc />
    public async Task<BinaryContent> ResolveAsync(object content, CancellationToken cancellationToken = default)
    {
        var url = (string)content;

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var filename = ExtractFilenameFromResponse(response, url);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            
            return new BinaryContent
            {
                Name = filename,
                Extension = contentType.GetExtensionFromContentType(),
                Stream = stream
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download file from URL: {Url}", url);
            throw;
        }
    }
    
    /// <summary>
    /// Extracts a filename from the HTTP response, either from Content-Disposition header or URL.
    /// </summary>
    private string ExtractFilenameFromResponse(HttpResponseMessage response, string url)
    {
        // Try to get filename from Content-Disposition header
        if (response.Content.Headers.ContentDisposition != null)
        {
            var contentDisposition = response.Content.Headers.ContentDisposition.ToString();
            const string filenameMarker = "filename=";
            var index = contentDisposition.IndexOf(filenameMarker, StringComparison.OrdinalIgnoreCase);
            
            if (index != -1)
            {
                var startIndex = index + filenameMarker.Length;
                var filename = contentDisposition[startIndex..].Trim(' ', '"', ';');
                if (!string.IsNullOrEmpty(filename))
                {
                    return filename;
                }
            }
        }

        // If no Content-Disposition header, extract from URL
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var filename = Path.GetFileName(path);
            
            if (!string.IsNullOrEmpty(filename) && Path.HasExtension(filename))
            {
                return filename;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to extract filename from URL: {Url}", url);
        }
        
        return "download";
    }
}
