using System.Text;
using Microsoft.Extensions.Logging;

namespace Elsa.IO.Services;

/// <summary>
/// Resolves various content types to streams.
/// </summary>
public class ContentResolver : IContentResolver
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<ContentResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentResolver"/> class.
    /// </summary>
    public ContentResolver(ILogger<ContentResolver> logger, IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string Name)> ResolveContentAsync(object content, string? name = null, CancellationToken cancellationToken = default)
    {
        switch (content)
        {
            case Stream stream:
                return (stream, name ?? "file.bin");

            case byte[] bytes:
                return (new MemoryStream(bytes), name ?? "file.bin");

            case string str when str.StartsWith("base64:"):
                var base64Content = str.Substring(7); // Remove "base64:" prefix
                var base64Bytes = Convert.FromBase64String(base64Content);
                return (new MemoryStream(base64Bytes), name ?? "file.bin");

            case string str when str.StartsWith("http://") || str.StartsWith("https://"):
                return await DownloadFromUrlAsync(str, name, cancellationToken);

            case string filePath when File.Exists(filePath):
                var fileName = name ?? Path.GetFileName(filePath);
                var fileStream = File.OpenRead(filePath);
                return (fileStream, fileName);

            case string textContent:
                var textBytes = Encoding.UTF8.GetBytes(textContent);
                return (new MemoryStream(textBytes), name ?? "file.txt");

            default:
                throw new ArgumentException($"Unsupported content type: {content?.GetType()?.Name ?? "null"}");
        }
    }

    private async Task<(Stream Stream, string Name)> DownloadFromUrlAsync(string url, string? name, CancellationToken cancellationToken)
    {
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
            var fileName = name ?? GetFileNameFromUrl(url) ?? "downloaded-file.bin";

            return (stream, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from URL: {Url}", url);
            throw;
        }
    }

    private static string? GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments;
            var lastSegment = segments.LastOrDefault();
            return string.IsNullOrEmpty(lastSegment) || lastSegment == "/" ? null : lastSegment;
        }
        catch
        {
            return null;
        }
    }
}