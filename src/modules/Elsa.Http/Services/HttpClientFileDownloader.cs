using Elsa.Http.Contracts;

namespace Elsa.Http.Services;

/// <summary>
/// A general-purpose downloader of files from a given URL that uses <see cref="HttpClient"/>.
/// </summary>
public class HttpClientFileDownloader : IFileDownloader
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientFileDownloader"/> class.
    /// </summary>
    public HttpClientFileDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> DownloadAsync(Uri url, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetAsync(url, cancellationToken);
    }
}