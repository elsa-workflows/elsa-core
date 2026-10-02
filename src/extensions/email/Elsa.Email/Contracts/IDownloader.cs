namespace Elsa.Email.Contracts;

/// <summary>
/// Download files from the Internet.
/// </summary>
public interface IDownloader
{
    /// <summary>
    /// Download whatever is returned at the specified URL.
    /// </summary>
    Task<HttpResponseMessage> DownloadAsync(Uri url, CancellationToken cancellationToken = default);
}