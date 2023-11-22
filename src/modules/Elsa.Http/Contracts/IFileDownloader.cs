using Elsa.Http.Options;

namespace Elsa.Http.Contracts;

/// <summary>
/// A general-purpose downloader of files from a given URL.
/// </summary>
public interface IFileDownloader
{
    /// <summary>
    /// Downloads a file from the specified URL.
    /// </summary>
    Task<HttpResponseMessage> DownloadAsync(Uri url, FileDownloadOptions? options = default, CancellationToken cancellationToken = default);    
}