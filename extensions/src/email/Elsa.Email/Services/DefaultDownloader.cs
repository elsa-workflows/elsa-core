using Elsa.Email.Contracts;

namespace Elsa.Email.Services;

/// <summary>
/// Download files from the Internet.
/// </summary>
public class DefaultDownloader : IDownloader
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    /// <summary>
    /// Download whatever is returned at the specified URL.
    /// </summary>
    public async Task<HttpResponseMessage> DownloadAsync(Uri url, CancellationToken cancellationToken = default) => await _httpClient.GetAsync(url, cancellationToken);
}