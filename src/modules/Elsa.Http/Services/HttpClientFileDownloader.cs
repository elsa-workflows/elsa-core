using Elsa.Http.Contracts;
using Elsa.Http.Options;

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
    public async Task<HttpResponseMessage> DownloadAsync(Uri url, FileDownloadOptions? options = default, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (options?.ETag != null)
            request.Headers.IfNoneMatch.Add(options.ETag);
        
        if(options?.Range != null)
            request.Headers.Range = options.Range;
        
        return await _httpClient.SendAsync(request, cancellationToken);
    }
}