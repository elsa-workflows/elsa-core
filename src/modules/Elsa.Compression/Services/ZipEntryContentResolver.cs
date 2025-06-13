using System.Text;
using Elsa.Compression.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Compression.Services;

/// <summary>
/// Resolves zip entry content to streams from various input formats.
/// </summary>
public class ZipEntryContentResolver : IZipEntryContentResolver
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<ZipEntryContentResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZipEntryContentResolver"/> class.
    /// </summary>
    public ZipEntryContentResolver(ILogger<ZipEntryContentResolver> logger, IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<Stream> ResolveContentAsync(ZipEntry entry, CancellationToken cancellationToken = default)
    {
        var (stream, _) = await ResolveContentAsync(entry.Content, entry.EntryName, cancellationToken);
        return stream;
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string EntryName)> ResolveContentAsync(object content, string? entryName = null, CancellationToken cancellationToken = default)
    {
        switch (content)
        {
            case Stream stream:
                return (stream, entryName ?? "file.bin");

            case byte[] bytes:
                return (new MemoryStream(bytes), entryName ?? "file.bin");

            case string str when str.StartsWith("base64:"):
                var base64Content = str.Substring(7); // Remove "base64:" prefix
                var base64Bytes = Convert.FromBase64String(base64Content);
                return (new MemoryStream(base64Bytes), entryName ?? "file.bin");

            case string str when str.StartsWith("http://") || str.StartsWith("https://"):
                return await DownloadFromUrlAsync(str, entryName, cancellationToken);

            case string filePath when File.Exists(filePath):
                var fileName = entryName ?? Path.GetFileName(filePath);
                var fileStream = File.OpenRead(filePath);
                return (fileStream, fileName);

            case string textContent:
                var textBytes = Encoding.UTF8.GetBytes(textContent);
                return (new MemoryStream(textBytes), entryName ?? "file.txt");

            default:
                throw new ArgumentException($"Unsupported content type: {content?.GetType()?.Name ?? "null"}");
        }
    }

    private async Task<(Stream Stream, string EntryName)> DownloadFromUrlAsync(string url, string? entryName, CancellationToken cancellationToken)
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
            var fileName = entryName ?? GetFileNameFromUrl(url) ?? "downloaded-file.bin";

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